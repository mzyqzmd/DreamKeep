using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>遗忘之魂：每消耗一张牌就对随机敌人造成伤害。</summary>
    [CreateAssetMenu(fileName = "ForgottenSoul", menuName = "DreamKeep/Data/Relic/遗忘之魂")]
    public sealed class SO_ForgottenSoul : SO_Relic
    {
        [SerializeField] private int _damage = 4;

        public int Damage => _damage;
    }
}
