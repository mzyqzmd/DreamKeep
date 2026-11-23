using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>皇家枕头：持有后在休息处休息时额外恢复固定生命值；本身不提供战斗效果，
    /// 回血加成由 RunRelicQueries 汇总后交给休息段结算与文案。</summary>
    [CreateAssetMenu(fileName = "RegalPillow", menuName = "DreamKeep/Data/Relic/皇家枕头")]
    public sealed class SO_RegalPillow : SO_Relic
    {
        [Min(1)] [SerializeField] private int _heal = 15;

        /// <summary>休息时额外恢复的生命值。</summary>
        public int Heal => _heal;
    }
}
