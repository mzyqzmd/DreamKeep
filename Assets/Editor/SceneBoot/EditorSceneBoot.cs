using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 编辑器 Play 入口固化：Unity 2022.3 的 Project Settings 并未暴露 Play Mode Start Scene 项，
/// 该能力只有 EditorSceneManager.playModeStartScene（会话级、不写项目文件）——故每次域重载时，
/// 从 EditorPrefs 读持久化的启动场景路径并自动指过去。
/// 想指定/修改启动场景：菜单 Tools/DreamKeep/Play 启动场景… 打开面板拖入场景资产。
/// </summary>
[InitializeOnLoad]
internal static class EditorSceneBoot
{
    private const string PrefsKey = "DreamKeep.PlayBootScene";

    static EditorSceneBoot()
    {
        string path = EditorPrefs.GetString(PrefsKey, string.Empty);
        if (string.IsNullOrEmpty(path))
            return;
        SceneAsset boot = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
        if (boot != null)
            EditorSceneManager.playModeStartScene = boot;
    }

    /// <summary>打开启动场景设置面板。</summary>
    [MenuItem("Tools/DreamKeep/ 设置编辑器默认启动场景")]
    private static void OpenBootScenePanel() => EditorSceneBootWindow.ShowWindow();

    /// <summary>将给定路径固化为默认启动场景；空串表示清除。</summary>
    internal static void ApplyBootScene(string path_)
    {
        if (string.IsNullOrEmpty(path_))
        {
            EditorSceneManager.playModeStartScene = null;
            EditorPrefs.DeleteKey(PrefsKey);
            Debug.Log("Play 启动场景已清除（进入 Play 将沿用当前打开场景）");
            return;
        }
        SceneAsset boot = AssetDatabase.LoadAssetAtPath<SceneAsset>(path_);
        if (boot == null)
        {
            Debug.LogError($"Play 启动场景无效：{path_}");
            return;
        }
        EditorSceneManager.playModeStartScene = boot;
        EditorPrefs.SetString(PrefsKey, path_);
        Debug.Log($"Play 启动场景已设为 {path_}（已记忆，本机后续会话自动生效）");
    }

    /// <summary>当前持久化的启动场景路径，无则返回空串。</summary>
    internal static string SavedBootScenePath => EditorPrefs.GetString(PrefsKey, string.Empty);
}
