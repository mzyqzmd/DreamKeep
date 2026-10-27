using UnityEditor;
using UnityEngine;

/// <summary>启动场景设置面板：拖入场景资产并固化为 Play 默认启动场景。</summary>
internal class EditorSceneBootWindow : EditorWindow
{
    private SceneAsset _sceneAsset;

    public static void ShowWindow()
    {
        EditorSceneBootWindow window = GetWindow<EditorSceneBootWindow>("Play 启动场景");
        window.minSize = new Vector2(360, 130);
    }

    private void OnEnable()
    {
        string saved = EditorSceneBoot.SavedBootScenePath;
        _sceneAsset = string.IsNullOrEmpty(saved) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(saved);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("拖入场景资产作为 Play 默认启动场景", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("设置后写入 EditorPrefs，本机后续 Unity 会话自动生效（会话级机制，不进版本库）。", MessageType.Info);

        SceneAsset next = (SceneAsset)EditorGUILayout.ObjectField("启动场景", _sceneAsset, typeof(SceneAsset), false);
        if (next != _sceneAsset)
        {
            _sceneAsset = next;
            EditorSceneBoot.ApplyBootScene(next == null ? string.Empty : AssetDatabase.GetAssetPath(next));
        }

        if (GUILayout.Button("清除（Play 沿用当前打开场景）"))
        {
            _sceneAsset = null;
            EditorSceneBoot.ApplyBootScene(string.Empty);
        }
    }
}
