using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「滑溜」定义：常驻状态，层数即可承受的压伤次数；
    /// 宿主每次将要失去生命值时由伤害修正通道压到 1 点，真实掉血后消费一层。</summary>
    [CreateAssetMenu(fileName = "Slippery", menuName = "DreamKeep/Data/Perk/滑溜")]
    public sealed class SO_Slippery : SO_Perk
    {
        public override string Name => "滑溜";
        public override EPerkSettleTiming SettleTiming => EPerkSettleTiming.None;
    }
}
