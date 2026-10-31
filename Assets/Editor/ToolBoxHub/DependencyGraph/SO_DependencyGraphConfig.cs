namespace ToolBoxHub.DependencyGraph
{
    /// <summary>
    /// 依赖关系图配置：懒加载单例（配置资产确保存在）。
    /// </summary>
    public class SO_DependencyGraphConfig : ScriptableObjectSingleton<SO_DependencyGraphConfig>
    {
        protected override string AssetPath => "Assets/Editor/ToolBoxHub/DependencyGraph/SO_DependencyGraphConfig.asset";
    }
}
