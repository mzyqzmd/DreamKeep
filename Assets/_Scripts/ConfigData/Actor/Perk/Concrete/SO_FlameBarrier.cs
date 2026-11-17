using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「火焰屏障」定义：本回合宿主每受到一次攻击，对攻击者造成等同层数的伤害，回合结束自动移除。</summary>
    [CreateAssetMenu(fileName = "FlameBarrier", menuName = "DreamKeep/Data/Perk/火焰屏障")]
    public sealed class SO_FlameBarrier : SO_Perk
    {
        public override string Name => "火焰屏障";
        public override EPerkSettleTiming SettleTiming => EPerkSettleTiming.TurnStart;
    }
}
