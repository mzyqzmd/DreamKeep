using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VContainer;
using _DreamKeep.CommonPresentation;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.GameRun;
using _DreamKeep.Presentation;
using _DreamKeep.RuntimeData;

/// <summary>开发面板的命令执行体：Play 模式下的测试操作（改血量金币、加遗物药水、战斗往手牌加卡等）与启动场景设置集中成静态入口，正式脚本不再挂 [Button]。
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

    #region 流程：加载各段

    /// <summary>加载首个战斗段（进段意图由 ESegmentKind 表达，守卫在 SegmentSceneManager 里）。</summary>
    internal static string StartFirstSegment()
    {
        SegmentSceneManager manager = Object.FindObjectOfType<SegmentSceneManager>();
        if (!manager) return Fail("场景里没有 SegmentSceneManager（请从场景 Scene_MainMenu 进 Play）");
        manager.StartSegmentAsync(ESegmentKind.Battle).Forget();
        return "已请求加载首个战斗段";
    }

    /// <summary>加载先古之民段（进段意图由 ESegmentKind 表达）。</summary>
    internal static string StartAncientSegment()
    {
        SegmentSceneManager manager = Object.FindObjectOfType<SegmentSceneManager>();
        if (!manager) return Fail("场景里没有 SegmentSceneManager（请从场景 Scene_MainMenu 进 Play）");
        manager.StartSegmentAsync(ESegmentKind.Ancient).Forget();
        return "已请求加载先古之民段";
    }

    /// <summary>加载休息段（进段意图由 ESegmentKind 表达）。</summary>
    internal static string StartRestSegment()
    {
        SegmentSceneManager manager = Object.FindObjectOfType<SegmentSceneManager>();
        if (!manager) return Fail("场景里没有 SegmentSceneManager（请从场景 Scene_MainMenu 进 Play）");
        manager.StartSegmentAsync(ESegmentKind.Rest).Forget();
        return "已请求加载休息段";
    }

    /// <summary>加载宝箱段（随机双池并出 1 件遗物，进段意图由 ESegmentKind 表达）。</summary>
    internal static string StartChestSegment()
    {
        SegmentSceneManager manager = Object.FindObjectOfType<SegmentSceneManager>();
        if (!manager) return Fail("场景里没有 SegmentSceneManager（请从场景 Scene_MainMenu 进 Play）");
        manager.StartSegmentAsync(ESegmentKind.Treasure).Forget();
        return "已请求加载宝箱段";
    }

    /// <summary>加载事件段（进入后随机掷一则已实装事件，进段意图由 ESegmentKind 表达）。</summary>
    internal static string StartEventSegment()
    {
        SegmentSceneManager manager = Object.FindObjectOfType<SegmentSceneManager>();
        if (!manager) return Fail("场景里没有 SegmentSceneManager（请从场景 Scene_MainMenu 进 Play）");
        manager.StartSegmentAsync(ESegmentKind.Event).Forget();
        return "已请求加载事件段";
    }

    /// <summary>加载商店段（进店掷一次货架，进段意图由 ESegmentKind 表达）。</summary>
    internal static string StartShopSegment()
    {
        SegmentSceneManager manager = Object.FindObjectOfType<SegmentSceneManager>();
        if (!manager) return Fail("场景里没有 SegmentSceneManager（请从场景 Scene_MainMenu 进 Play）");
        manager.StartSegmentAsync(ESegmentKind.Shop).Forget();
        return "已请求加载商店段";
    }

    #endregion

    #region 整局：血量与金币

    /// <summary>把玩家血量设为指定值：钳到 [0, MaxHp]；设 0 不触发死亡流程（死亡只在战斗动作链里裁决）。</summary>
    internal static string SetPlayerHp(int value_)
    {
        Creature creature = ResolvePlayerCreature();
        if (creature == null) return Fail("未找到 RunState（整局层是否已构建）");
        return WritePlayerHp(creature, value_);
    }

    /// <summary>按增量增减玩家血量：增减后钳到 [0, MaxHp]。</summary>
    internal static string ChangePlayerHp(int delta_)
    {
        Creature creature = ResolvePlayerCreature();
        if (creature == null) return Fail("未找到 RunState（整局层是否已构建）");
        return WritePlayerHp(creature, creature.CurrentHp + delta_);
    }

    /// <summary>把玩家血量回满：顶栏随 HpChanged 自动刷新，战斗内血条不订阅该事件，故补一次直写。</summary>
    internal static string RestoreFullHp()
    {
        Creature creature = ResolvePlayerCreature();
        if (creature == null) return Fail("未找到 RunState（整局层是否已构建）");
        return WritePlayerHp(creature, creature.MaxHp);
    }

    /// <summary>把金币设为指定值：结果钳到 >= 0。</summary>
    internal static string SetGold(int value_)
    {
        RunState run = ResolveRunState();
        if (run == null) return Fail("未找到 RunState（整局层是否已构建）");
        return WriteGold(run, value_);
    }

    /// <summary>按增量增减金币：结果钳到 >= 0，负增量减到 0 为止。</summary>
    internal static string ChangeGold(int delta_)
    {
        RunState run = ResolveRunState();
        if (run == null) return Fail("未找到 RunState（整局层是否已构建）");
        return WriteGold(run, run.Gold + delta_);
    }

    #endregion

    #region 物品：添加遗物与药水

    /// <summary>添加遗物：允许重复持有（去重只在抽取层）；顶栏遗物栏随 RelicAdded 自动补格。
    /// 战斗中已烘焙的遗物效果不随本操作增删，对下一场战斗才生效。</summary>
    internal static string AddRelic(SO_Relic relic_)
    {
        if (relic_ == null) return Fail("先在字段里拖入要添加的遗物");
        RunState run = ResolveRunState();
        if (run == null) return Fail("未找到 RunState（整局层是否已构建）");
        if (!run.Player.AddRelic(relic_)) return Fail("添加遗物失败");
        return $"已添加遗物「{relic_.name}」，当前 {run.Player.Relics.Count} 件";
    }

    /// <summary>添加药水：放进最靠前的空槽，三格已满被拒绝；顶栏药水栏随 PotionSlotChanged 自动刷新。</summary>
    internal static string AddPotion(SO_Potion potion_)
    {
        if (potion_ == null) return Fail("先在字段里拖入要添加的药水");
        RunState run = ResolveRunState();
        if (run == null) return Fail("未找到 RunState（整局层是否已构建）");
        if (!run.Player.TryAddPotion(potion_)) return Fail("药水槽已满（3/3），先腾出空格");
        return $"已添加药水「{potion_.name}」（第 {PotionSlotOf(run.Player, potion_) + 1} 格）";
    }

    #endregion

    #region 战斗：往手牌加卡

    /// <summary>是否处于战斗中：窗口据此把战斗小节置灰；战斗态由 StartCombat / EndCombat 开合。</summary>
    internal static bool IsInBattle()
    {
        RunState run = ResolveRunState();
        return run != null && run.Player.CombatState != null;
    }

    /// <summary>把指定配置卡现烤成战斗临时卡加入手牌：勾升级形态则先升级（无升级数据按基础形态加入）；
    /// 手牌满 10 张拒绝；该卡无局内卡映射，战斗结束自动消失、不污染局内卡组。</summary>
    internal static string AddCardToHand(SO_Card card_, bool upgraded_)
    {
        if (card_ == null) return Fail("先在字段里拖入要加入手牌的卡牌");
        IObjectResolver container = ResolveContainer();
        if (container == null) return Fail("未找到 RunState（整局层是否已构建）");

        BattleSession session = container.Resolve<BattleSession>();
        RunState run = container.Resolve<RunState>();
        if (session == null || !session.IsOpen || run.Player.CombatState == null)
            return Fail("不在战斗中，先进入战斗段");

        PlayerCombatState combat = run.Player.CombatState;
        if (combat.Hand.Count >= PlayerCombatState.MaxHandSize)
            return Fail($"手牌已满 {PlayerCombatState.MaxHandSize} 张，拒绝加入");

        // canonical 原型不可在运行时改动，先转可变副本再按需升级
        CardModel card = CardModelBaker.Bake(card_).ToMutable();
        bool upgraded = upgraded_ && card.CanUpgrade;
        if (upgraded) card.Upgrade();

        combat.Hand.Add(card);
        // 火后不理：手牌入场演出、可打态由既有 BE_CardDrawn 订阅链自动跟上；
        // 该卡并非来自抽牌堆，手牌旁抽牌堆计数会短暂 -1，回合开始的 SyncToLive 兜底对账自纠
        session.Events.PlayAsync(new BE_CardDrawn(card));

        string note = upgraded_ && !upgraded ? "（该卡无升级数据，按基础形态加入）" : string.Empty;
        return $"已把「{card.Name}」加入手牌（{combat.Hand.Count}/{PlayerCombatState.MaxHandSize}）{note}";
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

    // 血量公共段：钳到 [0, MaxHp] 后直写（顶栏随 HpChanged 自动刷），补刷战斗内血条；越界输入附钳制说明
    private static string WritePlayerHp(Creature creature_, int target_)
    {
        int clamped = Mathf.Clamp(target_, 0, creature_.MaxHp);
        creature_.CurrentHp = clamped;
        RefreshPlayerHealthBar(creature_);
        return clamped == target_
            ? $"玩家血量 {clamped}/{creature_.MaxHp}"
            : $"玩家血量 {clamped}/{creature_.MaxHp}（输入 {target_} 已钳到边界）";
    }

    // 金币公共段：钳到 >= 0 后直写（顶栏随 GoldChanged 自动刷）；越界输入附钳制说明
    private static string WriteGold(RunState run_, int target_)
    {
        run_.Gold = Mathf.Max(0, target_);
        return run_.Gold == target_ ? $"金币 {run_.Gold}" : $"金币 {run_.Gold}（输入 {target_} 已钳到 0）";
    }

    // 药水落格下标：TryAddPotion 成功后调用，理论上必命中，-1 仅防御异常路径
    private static int PotionSlotOf(Player player_, SO_Potion potion_)
    {
        for (int i = 0; i < player_.PotionSlots.Count; i++)
            if (player_.PotionSlots[i] == potion_) return i;
        return -1;
    }

    // 战斗内血条只由伤害 / 回血事件驱动，不订阅 HpChanged：直写血量后要自己补一次视图刷新
    private static void RefreshPlayerHealthBar(Creature creature_)
    {
        IObjectResolver container = ResolveContainer();
        if (container == null) return;
        BattleSession session = container.Resolve<BattleSession>();
        BattleStage stage = container.Resolve<BattleStage>();
        if (session == null || !session.IsOpen || stage == null || !stage.ActorRoot) return;
        if (!stage.ActorRoot.TryGetView(creature_, out UI_Actor view) || !view) return;
        UI_HealthBar healthBar = view.GetComponentInChildren<UI_HealthBar>(true);
        if (healthBar) healthBar.SetHealth(creature_.CurrentHp);
    }

    // 整局层容器：Build 之前 Container 为空
    private static IObjectResolver ResolveContainer()
    {
        RunLifetimeScope scope = Object.FindObjectOfType<RunLifetimeScope>();
        return scope ? scope.Container : null;
    }

    // 整局状态：容器存在即必已注册
    private static RunState ResolveRunState()
    {
        IObjectResolver container = ResolveContainer();
        return container == null ? null : container.Resolve<RunState>();
    }

    // 玩家生物：血量三命令的公共守卫段
    private static Creature ResolvePlayerCreature()
    {
        RunState run = ResolveRunState();
        return run?.Player.Creature;
    }

    private static string Fail(string reason_)
    {
        Debug.LogWarning(LogPrefix + reason_);
        return reason_;
    }
}
