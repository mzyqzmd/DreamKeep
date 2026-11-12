using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「失衡」定义：精英专属常驻状态，宿主的攻击被目标完全格挡时自击晕。</summary>
    [CreateAssetMenu(fileName = "Imbalanced", menuName = "DreamKeep/Data/Perk/失衡")]
    public sealed class SO_Imbalanced : SO_Perk
    {
        public override string Name => "失衡";
        public override EPerkSettleTiming SettleTiming => EPerkSettleTiming.None;
    }
}