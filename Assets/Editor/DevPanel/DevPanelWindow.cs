using UnityEditor;
using UnityEngine;
using _DreamKeep.ConfigData;

/// <summary>开发面板：上区「运行时」集中 Play 期测试操作（卡牌 / 流程 / 整局 / 物品 / 战斗五小节，非 Play 整区置灰），
/// 下区「启动场景」设 Play 默认启动场景（始终可用）；底部一行显示最近一次操作结果。</summary>
internal sealed class DevPanelWindow : EditorWindow
{
    private const int DefaultGoldAmount = 100;
    private const int DefaultHpAmount = 10;

    private int _goldAmount = DefaultGoldAmount;
    private int _hpAmount = DefaultHpAmount;
    private SO_Relic _relicToAdd;
    private SO_Potion _potionToAdd;
    private SO_Card _cardToAdd;
    private bool _cardUpgraded;
    private string _lastResult = "等待操作";
    private SceneAsset _bootScene;

    internal static void ShowWindow()
    {
        DevPanelWindow window = GetWindow<DevPanelWindow>("开发面板");
        window.minSize = new Vector2(380f, 560f);
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
            if (GUILayout.Button("进入宝箱段")) _lastResult = DevPanelCommands.StartChestSegment();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("整局", EditorStyles.miniBoldLabel);
            DrawPlayerHpRow();
            if (GUILayout.Button("玩家回满血")) _lastResult = DevPanelCommands.RestoreFullHp();
            DrawGoldRow();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("物品", EditorStyles.miniBoldLabel);
            DrawItemsSection();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("战斗", EditorStyles.miniBoldLabel);
            DrawAddCardSection();
        }
    }

    // 血量行：一个输入值服务「设为」「增加」两个入口；边界语义必须提示开发者
    private void DrawPlayerHpRow()
    {
        EditorGUILayout.HelpBox("血量钳到 [0, MaxHp]；设 0 不触发死亡流程，回合照常推进。", MessageType.None);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("血量值");
        _hpAmount = EditorGUILayout.IntField(_hpAmount);
        if (GUILayout.Button("设为", GUILayout.Width(44f))) _lastResult = DevPanelCommands.SetPlayerHp(_hpAmount);
        if (GUILayout.Button("增加", GUILayout.Width(44f))) _lastResult = DevPanelCommands.ChangePlayerHp(_hpAmount);
        EditorGUILayout.EndHorizontal();
    }

    // 金币行：同款双入口；负增量减到 0 为止
    private void DrawGoldRow()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("金币值");
        _goldAmount = EditorGUILayout.IntField(_goldAmount);
        if (GUILayout.Button("设为", GUILayout.Width(44f))) _lastResult = DevPanelCommands.SetGold(_goldAmount);
        if (GUILayout.Button("增加", GUILayout.Width(44f))) _lastResult = DevPanelCommands.ChangeGold(_goldAmount);
        EditorGUILayout.EndHorizontal();
    }

    // 物品小节：只做添加——遗物已持有被去重、药水三格满被拒绝，结果见「最近操作」行
    private void DrawItemsSection()
    {
        _relicToAdd = (SO_Relic)EditorGUILayout.ObjectField("遗物", _relicToAdd, typeof(SO_Relic), false);
        if (GUILayout.Button("添加遗物")) _lastResult = DevPanelCommands.AddRelic(_relicToAdd);

        _potionToAdd = (SO_Potion)EditorGUILayout.ObjectField("药水", _potionToAdd, typeof(SO_Potion), false);
        if (GUILayout.Button("添加药水")) _lastResult = DevPanelCommands.AddPotion(_potionToAdd);
    }

    // 战斗小节：非战斗置灰；加入的卡是战斗临时卡，战斗结束自动消失
    private void DrawAddCardSection()
    {
        // 先短路 playing 再查战斗态：编辑态不必每次 OnGUI 全场找 RunLifetimeScope
        bool inBattle = EditorApplication.isPlaying && DevPanelCommands.IsInBattle();
        using (new EditorGUI.DisabledScope(!inBattle))
        {
            EditorGUILayout.HelpBox("加入的卡是战斗临时卡：战斗结束自动消失，不写回局内卡组；手牌满 10 张拒绝。", MessageType.None);
            _cardToAdd = (SO_Card)EditorGUILayout.ObjectField("卡牌", _cardToAdd, typeof(SO_Card), false);
            _cardUpgraded = EditorGUILayout.Toggle("升级形态", _cardUpgraded);
            if (GUILayout.Button("加入手牌")) _lastResult = DevPanelCommands.AddCardToHand(_cardToAdd, _cardUpgraded);
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
