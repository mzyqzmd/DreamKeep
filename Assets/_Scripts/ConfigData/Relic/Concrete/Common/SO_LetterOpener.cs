using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>开信刀：每回合每打出若干张技能牌，对全体敌人造成伤害。</summary>
    [CreateAssetMenu(fileName = "LetterOpener", menuName = "DreamKeep/Data/Relic/开信刀")]
    public sealed class SO_LetterOpener : SO_Relic
    {
        [SerializeField] private int _count = 3;
        [SerializeField] private int _damage = 5;

        public int Count => _count;
        public int Damage => _damage;
    }
}
