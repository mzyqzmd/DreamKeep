using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>锁镰：每回合每打出若干张攻击牌，对随机敌人造成伤害。</summary>
    [CreateAssetMenu(fileName = "Kusarigama", menuName = "DreamKeep/Data/Relic/锁镰")]
    public sealed class SO_Kusarigama : SO_Relic
    {
        [SerializeField] private int _count = 3;
        [SerializeField] private int _damage = 4;

        public int Count => _count;
        public int Damage => _damage;
    }
}
