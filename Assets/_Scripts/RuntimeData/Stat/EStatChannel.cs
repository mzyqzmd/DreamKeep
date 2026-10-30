namespace _DreamKeep.RuntimeData
{
    /// <summary>数值通道：StatHook 与描述渲染器按通道取用。Damage 与 Block 是战斗内可被 perk 修正的值通道
    /// （经预览门面求最终值）；Cost/PerkLayers/DrawCard/LostHealth/DamageTimes 是静态比较通道（比较基准与当前值
    /// 都由调用方给出，不经门面求值）。着色方向由渲染器按通道分派：伤害/格挡高绿低红、层数/抽牌/次数仅升绿、
    /// 失血低绿高红、费用仅降绿。</summary>
    public enum EStatChannel
    {
        Damage,
        Block,
        Cost,
        PerkLayers,
        DrawCard,
        LostHealth,
        DamageTimes,
    }
}
