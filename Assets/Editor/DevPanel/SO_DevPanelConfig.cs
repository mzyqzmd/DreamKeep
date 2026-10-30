using UnityEditor;
using UnityEngine;

/// <summary>开发面板的配置资产：启动场景随工程走，团队每次进 Play 都从同一场景启动。
/// 用经典 ScriptableObject + AssetDatabase 懒加载（资产缺失时现场创建），与 ToolBoxHub 版式一致。</summary>
internal class SO_DevPanelConfig : ScriptableObject
{
    // 资产存放路径（放在本工具目录下）
    private const string _assetPath = "Assets/Editor/DevPanel/DevPanelConfig.asset";

    private static SO_DevPanelConfig _instance;

    [SerializeField] private SceneAsset _bootScene;

    internal static SO_DevPanelConfig Instance
    {
        get
        {
            if (_instance) return _instance;
            _instance = AssetDatabase.LoadAssetAtPath<SO_DevPanelConfig>(_assetPath);
            if (_instance) return _instance;
            _instance = CreateInstance<SO_DevPanelConfig>();
            AssetDatabase.CreateAsset(_instance, _assetPath);
            AssetDatabase.SaveAssets();
            return _instance;
        }
    }

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
