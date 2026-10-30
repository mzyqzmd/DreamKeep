using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>水银沙漏：每个玩家回合开始时对全体敌人造成伤害。</summary>
    [CreateAssetMenu(fileName = "MercuryHourglass", menuName = "DreamKeep/Data/Relic/水银沙漏")]
    public sealed class SO_MercuryHourglass : SO_Relic
    {
        [SerializeField] private int _damage = 3;

        public int Damage => _damage;
    }
}
