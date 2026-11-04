namespace _DreamKeep.ConfigData
{
    /// <summary>节点可达性四态（同文档 MapPointState）：仅供 UI 展示，不参与生成。</summary>
    public enum EMapNodeState
    {
        Travelable = 1,    // 已通过节点的子节点：可选
        Traveled = 2,      // 已通过（内容已完成）
        Untravelable = 3,  // 不可达
        InProgress = 4,    // 已进入、内容尚未完成：地图上保持全亮，由当前位置圆环标识
    }
}
