using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「横冲直撞」定义：仪式兽专属常驻状态，层数即血量阈值，战斗中不增不减；
    /// 受到实际伤害掉血且生命值不高于层数时，由 GameBattle 的触发链击晕宿主并切阶段二。</summary>
    [CreateAssetMenu(fileName = "Rampage", menuName = "DreamKeep/Data/Perk/横冲直撞")]
    public sealed class SO_Rampage : SO_Perk
    {
        public override string Name => "横冲直撞";
        public override EPerkSettleTiming SettleTiming => EPerkSettleTiming.None;
    }
}
