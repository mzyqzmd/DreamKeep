using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>卡戎之灰：每消耗一张牌就对全体敌人造成伤害。</summary>
    [CreateAssetMenu(fileName = "CharonsAshes", menuName = "DreamKeep/Data/Relic/卡戎之灰")]
    public sealed class SO_CharonsAshes : SO_Relic
    {
        [SerializeField] private int _damage = 2;

        public int Damage => _damage;
    }
}
