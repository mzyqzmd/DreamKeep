using UnityEditor;
using UnityEngine;

/// <summary>项目专属开发面板：Play 模式下集中提供卡牌升级、加载首个战斗段、加金币、回满血四项测试操作。
/// 非 Play 时全部按钮置灰（四项都依赖运行时场景对象）；底部一行显示最近一次操作结果。</summary>
internal sealed class EditorDevPanelWindow : EditorWindow
{
    private const int DefaultGoldAmount = 100;

    private int _goldAmount = DefaultGoldAmount;
    private string _lastResult = "等待操作";

    [MenuItem("Tools/DreamKeep/开发面板")]
    private static void Open()
    {
        EditorDevPanelWindow window = GetWindow<EditorDevPanelWindow>("开发面板");
        window.minSize = new Vector2(360f, 260f);
    }

    private void OnGUI()
    {
        bool playing = EditorApplication.isPlaying;
        if (!playing)
            EditorGUILayout.HelpBox("仅在 Play 模式下可用：面板操作的是运行时整局状态与战斗段。", MessageType.Info);

        using (new EditorGUI.DisabledScope(!playing))
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("卡牌", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("先在 Hierarchy 里选中卡牌对象；未绑定卡牌的脸（同一张卡下的另外三张）会被跳过。",
                MessageType.None);
            if (GUILayout.Button("升级选中的卡面")) _lastResult = EditorDevPanel.UpgradeSelectedCardFaces();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("流程", EditorStyles.boldLabel);
            if (GUILayout.Button("开始首个战斗段")) _lastResult = EditorDevPanel.StartFirstSegment();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("整局", EditorStyles.boldLabel);
            _goldAmount = EditorGUILayout.IntField("金币增量", _goldAmount);
            if (GUILayout.Button("加金币")) _lastResult = EditorDevPanel.AddGold(_goldAmount);
            if (GUILayout.Button("玩家回满血")) _lastResult = EditorDevPanel.RestoreFullHp();
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField("最近操作", _lastResult, EditorStyles.miniLabel);
    }
}
