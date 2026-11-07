namespace _DreamKeep.ConfigData
{
    /// <summary>地图节点类型：决定进入时切入的段场景与遭遇种类；None 供"未揭示"与生成中的空占位。
    /// 数值已落盘到 MapNodeVisual.asset（_type）：只允许追加，不得改已有值或重排。</summary>
    public enum EMapPointType
    {
        None = 0,
        Ancient = 1,    // 起点（先古之民）
        Monster = 2,    // 普通战斗
        Elite = 3,      // 精英战斗
        Shop = 4,       // 商店
        Rest = 5,       // 休息
        Chest = 6,   // 宝箱
        Unknown = 7,      // 未知（"?"，进入才揭晓——即文档的 Unknown）
        Boss = 8,       // 首领
    }
}
