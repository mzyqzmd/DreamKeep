namespace _DreamKeep.ConfigData
{
    /// <summary>卡牌值通道：升级补丁用它指明"改的是哪个数值"，具体落在哪条词条上由烘焙期按通道匹配。
    /// 状态的施加层数住在状态原型上、不在数值容器之列，由独立的 UD_ModifyPerkLayers 表达。</summary>
    public enum EUpgradeChannel
    {
        Cost,          // 费用（卡级，不落在词条上）
        Damage,        // 伤害值（伤害词条）
        DamageTimes,   // 伤害次数（伤害词条）
        Block,         // 格挡值（格挡词条）
        DrawCount,     // 抽牌数（抽牌词条）
        EnergyGain,    // 获得能量（获得能量词条）
        Heal,          // 治疗量（治疗词条）
        SelectCount,   // 选牌目标张数（选择升级 / 选择消耗词条）
    }
}
