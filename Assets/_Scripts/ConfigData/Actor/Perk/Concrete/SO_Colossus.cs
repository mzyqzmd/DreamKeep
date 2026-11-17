using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>状态「巨像」定义：本回合内有易伤状态的敌人对宿主的伤害按比率降低，回合结束自动移除。</summary>
    [CreateAssetMenu(fileName = "Colossus", menuName = "DreamKeep/Data/Perk/巨像")]
    public sealed class SO_Colossus : SO_Perk
    {
        [SerializeField] private float _damageReduction = 0.5f;

        public float DamageReduction => _damageReduction;

        public override string Name => "巨像";
        public override EPerkSettleTiming SettleTiming => EPerkSettleTiming.TurnStart;
    }
}