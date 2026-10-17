using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「再生」定义：回合结束恢复等于层数的生命，随后层数递减。</summary>
    [CreateAssetMenu(fileName = "Regen", menuName = "DreamKeep/Data/Perk/再生")]
    public sealed class SO_Regen : SO_Perk
    {
        public override string Name => "再生";
        public override EPerkSettleTiming SettleTiming => EPerkSettleTiming.TurnEnd;
    }
}
