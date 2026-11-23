using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「缓冲」定义：宿主下一次受到伤害时改为只失去 1 点生命，消耗一层；常驻直到用完。</summary>
    [CreateAssetMenu(fileName = "Buffer", menuName = "DreamKeep/Data/Perk/缓冲")]
    public sealed class SO_Buffer : SO_Perk
    {
        public override string Name => "缓冲";
        public override EPerkSettleTiming SettleTiming => EPerkSettleTiming.None;
    }
}
