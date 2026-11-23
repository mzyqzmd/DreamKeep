using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「光辉」定义：宿主接下来若干回合开始时额外获得 1 能量，每回合递减一层。</summary>
    [CreateAssetMenu(fileName = "Radiance", menuName = "DreamKeep/Data/Perk/光辉")]
    public sealed class SO_Radiance : SO_Perk
    {
        public override string Name => "光辉";
        public override EPerkSettleTiming SettleTiming => EPerkSettleTiming.TurnStart;
    }
}
