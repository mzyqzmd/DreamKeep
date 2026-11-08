using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>开发面板的编辑器钩子：Unity 2022.3 的 Project Settings 并未暴露 Play Mode Start Scene 项，
/// 该能力只有 EditorSceneManager.playModeStartScene（会话级、不写项目文件）——故每次域重载时，
/// 把 DevPanelConfig.asset 里记的启动场景重新指过去。
/// 实际动作走 delayCall 而非静态构造体：首次需要现场创建配置资产，避开资源导入阶段。</summary>
[InitializeOnLoad]
internal static class DevPanelHook
{
    static DevPanelHook() => EditorApplication.delayCall += ApplyBootScene;

    private static void ApplyBootScene()
    {
        SO_DevPanelConfig config = SO_DevPanelConfig.Instance;
        if (config.BootScene) EditorSceneManager.playModeStartScene = config.BootScene;
    }
}
