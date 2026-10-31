using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VContainer;
using _DreamKeep.CommonPresentation;
using _DreamKeep.GameRun;
using _DreamKeep.Presentation;
using _DreamKeep.RuntimeData;

/// <summary>开发面板的命令执行体：Play 模式下的四项测试操作与启动场景设置集中成静态入口，正式脚本不再挂 [Button]。
/// 每次点击现查运行时对象、不缓存引用——退出 Play 后场景引用会变成伪空。
/// 所有查找失败只告警并返回原因文案，不抛异常（调用方是 EditorWindow.OnGUI，异常会中断整窗绘制）。</summary>
internal static class DevPanelCommands
{
    private const string LogPrefix = "[开发面板] ";

    // 菜单唯一入口：打开开发面板窗口
    [MenuItem("Tools/DreamKeep/开发面板")]
    private static void OpenWindow() => DevPanelWindow.ShowWindow();

    #region 卡牌：升级选中的卡面

    /// <summary>升级选中对象上的全部卡面。一张战斗卡下有四张脸实例、只有一张被 Bind，未绑定的跳过。</summary>
    internal static string UpgradeSelectedCardFaces()
    {
        GameObject[] selected = Selection.gameObjects;
        if (selected.Length == 0) return Fail("先在 Hierarchy 里选中含卡面的对象");

        List<string> upgraded = new();
        foreach (GameObject root in selected)
        foreach (UI_CardFace face in root.GetComponentsInChildren<UI_CardFace>(true))
        {
            if (face.Card == null) continue; // 其余三张脸没有绑定卡牌，Upgrade 内部不做空判
            face.Card.Upgrade();
            upgraded.Add(face.Card.Name);
        }

        return upgraded.Count == 0
            ? Fail("选中的对象上没有已绑定卡牌的卡面")
            : $"已升级 {upgraded.Count} 张：{string.Join("、", upgraded)}";
    }

    #endregion

    #region 流程：加载首个战斗段

    /// <summary>加载首个战斗段（段名与守卫都在 SegmentSceneManager 里）。</summary>
    internal static string StartFirstSegment()
    {
        SegmentSceneManager manager = Object.FindObjectOfType<SegmentSceneManager>();
        if (!manager) return Fail("场景里没有 SegmentSceneManager（请从场景 Scene_MainMenu 进 Play）");
        manager.StartFirstSegment();
        return "已请求加载首个战斗段";
    }

    /// <summary>加载先古之民段（段名与守卫都在 SegmentSceneManager 里）。</summary>
    internal static string StartAncientSegment()
    {
        SegmentSceneManager manager = Object.FindObjectOfType<SegmentSceneManager>();
        if (!manager) return Fail("场景里没有 SegmentSceneManager（请从场景 Scene_MainMenu 进 Play）");
        manager.StartSegment(SegmentSceneManager.AncientSegmentName);
        return "已请求加载先古之民段";
    }

    /// <summary>加载休息段（段名与守卫都在 SegmentSceneManager 里）。</summary>
    internal static string StartRestSegment()
    {
        SegmentSceneManager manager = Object.FindObjectOfType<SegmentSceneManager>();
        if (!manager) return Fail("场景里没有 SegmentSceneManager（请从场景 Scene_MainMenu 进 Play）");
        manager.StartSegment(SegmentSceneManager.RestSegmentName);
        return "已请求加载休息段";
    }

    #endregion

    #region 整局：加金币与回满血

    /// <summary>按给定增量加金币：Gold 的 setter 内自带变更通知，顶栏自动刷新。</summary>
    internal static string AddGold(int amount_)
    {
        IObjectResolver container = ResolveContainer();
        if (container == null) return Fail("未找到 RunState（整局层是否已构建）");
        RunState run = container.Resolve<RunState>();
        run.Gold += amount_;
        return $"金币 {run.Gold}（本次 {(amount_ >= 0 ? "+" : string.Empty)}{amount_}）";
    }

    /// <summary>把玩家血量回满：顶栏随 HpChanged 自动刷新，战斗内血条不订阅该事件，故补一次直写。</summary>
    internal static string RestoreFullHp()
    {
        IObjectResolver container = ResolveContainer();
        if (container == null) return Fail("未找到 RunState（整局层是否已构建）");
        Creature creature = container.Resolve<RunState>().Player.Creature;
        creature.CurrentHp = creature.MaxHp;
        RefreshPlayerHealthBar(creature);
        return $"玩家血量 {creature.CurrentHp}/{creature.MaxHp}";
    }

    #endregion

    #region 启动场景：写入配置并立即生效

    /// <summary>写入默认启动场景并立刻应用到本次会话；传 null 表示清除。</summary>
    internal static string ApplyBootScene(SceneAsset scene_)
    {
        SO_DevPanelConfig.Instance.SetBootScene(scene_);
        EditorSceneManager.playModeStartScene = scene_;
        return scene_ ? $"启动场景 {scene_.name}" : "启动场景已清除（进入 Play 沿用当前打开场景）";
    }

    #endregion

    // 战斗内血条只由伤害 / 回血事件驱动，不订阅 HpChanged：直写血量后要自己补一次视图刷新
    private static void RefreshPlayerHealthBar(Creature creature_)
    {
        IObjectResolver container = ResolveContainer();
        if (container == null) return;
        BattleSession session = container.Resolve<BattleSession>();
        if (session == null || !session.IsOpen || !session.ActorRoot) return;
        if (!session.ActorRoot.TryGetView(creature_, out UI_Actor view) || !view) return;
        UI_HealthBar healthBar = view.GetComponentInChildren<UI_HealthBar>(true);
        if (healthBar) healthBar.SetHealth(creature_.CurrentHp);
    }

    // 整局层容器：Build 之前 Container 为空
    private static IObjectResolver ResolveContainer()
    {
        RunLifetimeScope scope = Object.FindObjectOfType<RunLifetimeScope>();
        return scope ? scope.Container : null;
    }

    private static string Fail(string reason_)
    {
        Debug.LogWarning(LogPrefix + reason_);
        return reason_;
    }
}
