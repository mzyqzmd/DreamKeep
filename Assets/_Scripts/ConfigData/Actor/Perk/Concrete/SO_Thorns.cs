using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「荆棘」定义：宿主受到攻击时对攻击者造成等同层数的伤害，常驻不衰减。</summary>
    [CreateAssetMenu(fileName = "Thorns", menuName = "DreamKeep/Data/Perk/荆棘")]
    public sealed class SO_Thorns : SO_Perk
    {
        public override string Name => "荆棘";
        public override EPerkSettleTiming SettleTiming => EPerkSettleTiming.None;
    }
}
