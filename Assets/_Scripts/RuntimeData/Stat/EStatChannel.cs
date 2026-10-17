namespace _DreamKeep.RuntimeData
{
    /// <summary>数值通道：同一个状态可以对不同通道给出不同修正，Hook 与描述渲染器按通道取用。
    /// Cost 与 PerkLayers 是词条内的静态比较通道（比较基准与当前值都由调用方给出，不经门面求值）：
    /// 费用越低越好、层数越高越好，两条方向相反的着色规则由渲染器按通道分派。</summary>
    public enum EStatChannel
    {
        Damage,
        Block,
        Cost,
        PerkLayers,
    }
}
