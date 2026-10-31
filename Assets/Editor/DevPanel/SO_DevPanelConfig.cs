using UnityEditor;
using UnityEngine;
using ToolBoxHub;

/// <summary>开发面板的配置资产：启动场景随工程走，团队每次进 Play 都从同一场景启动。
/// 懒加载单例由 ScriptableObjectSingleton 基座提供（资产缺失时现场创建）。</summary>
internal class SO_DevPanelConfig : ScriptableObjectSingleton<SO_DevPanelConfig>
{
    protected override string AssetPath => "Assets/Editor/DevPanel/DevPanelConfig.asset";

    [SerializeField] private SceneAsset _bootScene;

    #region 启动场景
    internal SceneAsset BootScene => _bootScene;

    /// <summary>写入启动场景并落盘；传 null 表示清除。</summary>
    internal void SetBootScene(SceneAsset scene_)
    {
        _bootScene = scene_;
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }
    #endregion
}
