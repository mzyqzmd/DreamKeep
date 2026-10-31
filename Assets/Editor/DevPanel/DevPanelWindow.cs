using UnityEditor;
using UnityEngine;

/// <summary>开发面板：上区「运行时」集中四项 Play 期测试操作（非 Play 整区置灰），
/// 下区「启动场景」设 Play 默认启动场景（始终可用）；底部一行显示最近一次操作结果。</summary>
internal sealed class DevPanelWindow : EditorWindow
{
    private const int DefaultGoldAmount = 100;

    private int _goldAmount = DefaultGoldAmount;
    private string _lastResult = "等待操作";
    private SceneAsset _bootScene;

    internal static void ShowWindow()
    {
        DevPanelWindow window = GetWindow<DevPanelWindow>("开发面板");
        window.minSize = new Vector2(380f, 420f);
    }

    private void OnEnable() => _bootScene = SO_DevPanelConfig.Instance.BootScene;

    private void OnGUI()
    {
        DrawRuntimeSection();
        DrawSeparator();
        DrawBootSceneSection();
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField("最近操作", _lastResult, EditorStyles.miniLabel);
    }

    private void DrawRuntimeSection()
    {
        bool playing = EditorApplication.isPlaying;
        EditorGUILayout.LabelField("运行时", EditorStyles.boldLabel);
        if (!playing) EditorGUILayout.HelpBox("仅在 Play 模式下可用：本区操作的是运行时整局状态与战斗段。", MessageType.Info);

        using (new EditorGUI.DisabledScope(!playing))
        {
            EditorGUILayout.LabelField("卡牌", EditorStyles.miniBoldLabel);
            EditorGUILayout.HelpBox("先在 Hierarchy 里选中卡牌对象；未绑定卡牌的脸（同一张卡下的另外三张）会被跳过。", MessageType.None);
            if (GUILayout.Button("升级选中的卡面")) _lastResult = DevPanelCommands.UpgradeSelectedCardFaces();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("流程", EditorStyles.miniBoldLabel);
            if (GUILayout.Button("开始首个战斗段")) _lastResult = DevPanelCommands.StartFirstSegment();
            if (GUILayout.Button("进入先古之民")) _lastResult = DevPanelCommands.StartAncientSegment();
            if (GUILayout.Button("进入休息段")) _lastResult = DevPanelCommands.StartRestSegment();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("整局", EditorStyles.miniBoldLabel);
            _goldAmount = EditorGUILayout.IntField("金币增量", _goldAmount);
            if (GUILayout.Button("加金币")) _lastResult = DevPanelCommands.AddGold(_goldAmount);
            if (GUILayout.Button("玩家回满血")) _lastResult = DevPanelCommands.RestoreFullHp();
        }
    }

    private void DrawBootSceneSection()
    {
        EditorGUILayout.LabelField("启动场景", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Play 默认启动场景，记在 DevPanelConfig.asset（随工程走，进版本库）。", MessageType.None);

        SceneAsset next = (SceneAsset)EditorGUILayout.ObjectField("启动场景", _bootScene, typeof(SceneAsset), false);
        if (next != _bootScene)
        {
            _bootScene = next;
            _lastResult = DevPanelCommands.ApplyBootScene(next);
        }

        if (GUILayout.Button("清除（Play 沿用当前打开场景）")) _lastResult = DevPanelCommands.ApplyBootScene(null);
    }

    private static void DrawSeparator()
    {
        EditorGUILayout.Space(8);
        EditorGUI.DrawRect(GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true)), new Color(0.35f, 0.35f, 0.35f));
        EditorGUILayout.Space(8);
    }
}
