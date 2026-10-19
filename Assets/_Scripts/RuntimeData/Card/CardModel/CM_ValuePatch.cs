using _DreamKeep.ConfigData;
using _DreamKeep.Utility;

namespace _DreamKeep.RuntimeData
{
    /// <summary>升级补丁·改数值（运行时形态）：指明改哪条词条的哪个数值容器、以及加多少。
    /// 通道到数值容器的映射由本类提供，烘焙期的补丁匹配与运行时的补丁应用共用同一份真源；
    /// 卡级费用（下标 -1）与"施加状态层数"（数值容器是状态原型，不是 ModifiableStat）由 Kind 单独分派，
    /// 其中层数补丁以 null 表示"改的是状态原型的层数"。</summary>
    internal sealed class CM_ValuePatch
    {
        internal EUpgradeChannel? Kind { get; }

        /// <summary>目标词条在基础词条列表中的下标；卡级费用为 -1（不落在词条上）。</summary>
        internal int AffixIndex { get; }

        internal StatModifier Modifier { get; }

        internal CM_ValuePatch(EUpgradeChannel? kind_, int affixIndex_, StatModifier modifier_)
        {
            Kind = kind_;
            AffixIndex = affixIndex_;
            Modifier = modifier_;
        }

        #region 值通道 → 该词条承载的数值容器（费用与"施加状态层数"不在此列，取不到即返回 null）

        internal static ModifiableStat StatOf(EUpgradeChannel kind_, CardAffix affix_) => kind_ switch
        {
            EUpgradeChannel.Damage => affix_ is CA_Damage damage ? damage.Amount : null,
            EUpgradeChannel.DamageTimes => affix_ is CA_Damage damage ? damage.Times : null,
            EUpgradeChannel.Block => affix_ is CA_Block block ? block.Amount : null,
            EUpgradeChannel.DrawCount => affix_ is CA_DrawCard drawCard ? drawCard.Count : null,
            EUpgradeChannel.EnergyGain => affix_ is CA_GainEnergy gainEnergy ? gainEnergy.Amount : null,
            _ => null,
        };

        #endregion
    }
}
