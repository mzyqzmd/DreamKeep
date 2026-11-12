using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「高墙」定义：辅助型敌人携带的常驻状态，宿主回合结束时为自己与所有存活盟友累加格挡，层数不递减。</summary>
    [CreateAssetMenu(fileName = "Rampart", menuName = "DreamKeep/Data/Perk/高墙")]
    public sealed class SO_Rampart : SO_Perk
    {
        public override string Name => "高墙";
        public override EPerkSettleTiming SettleTiming => EPerkSettleTiming.TurnEnd;
    }
}
