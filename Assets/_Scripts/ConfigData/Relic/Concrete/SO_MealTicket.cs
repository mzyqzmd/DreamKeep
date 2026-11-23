using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>餐券：持有后进入商店时恢复固定生命值；本身不提供战斗效果，进店回血由 RunRelicQueries 汇总后交给商店段结算。</summary>
    [CreateAssetMenu(fileName = "MealTicket", menuName = "DreamKeep/Data/Relic/餐券")]
    public sealed class SO_MealTicket : SO_Relic
    {
        [Min(1)] [SerializeField] private int _heal = 15;

        /// <summary>进入商店时恢复的生命值。</summary>
        public int Heal => _heal;
    }
}
