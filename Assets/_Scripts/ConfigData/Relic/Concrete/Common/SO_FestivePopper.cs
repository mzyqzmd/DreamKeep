using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>节日拉炮：战斗开始时对全体敌人造成伤害（遗物伤害不吃力量与易伤）。</summary>
    [CreateAssetMenu(fileName = "FestivePopper", menuName = "DreamKeep/Data/Relic/节日拉炮")]
    public sealed class SO_FestivePopper : SO_Relic
    {
        [SerializeField] private int _damage = 5;

        public int Damage => _damage;
    }
}
