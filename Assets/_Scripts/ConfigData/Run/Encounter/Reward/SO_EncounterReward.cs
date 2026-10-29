using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>遭遇奖励配置：一场战斗打完给什么——金币区间、卡牌奖励组数（每组恒三选一）、药水掉落概率与遗物件数，以及三类抽取池。
    /// 战斗独有，由 <see cref="SO_Encounter"/> 引用；Boss 等终局遭遇不配（打完即结束），留空即不结算。</summary>
    [CreateAssetMenu(fileName = "Reward", menuName = "DreamKeep/Data/Run/Encounter/Reward")]
    public sealed class SO_EncounterReward : ScriptableObject
    {
        [Header("金币")]
        [SerializeField] private int _goldMin = 15;
        [SerializeField] private int _goldMax = 25;

        [Header("卡牌奖励")]
        [Tooltip("给几组卡牌奖励：每组固定三选一")]
        [SerializeField] private int _cardRewardCount = 1;

        [SerializeField] private SO_CardPool _cardPool;

        [Header("药水奖励")]
        [Range(0f, 1f)]
        [SerializeField] private float _potionChance = 0.3f;

        [SerializeField] private SO_PotionPool _potionPool;

        [Header("遗物奖励")]
        [Tooltip("给几件遗物：普通遭遇填 0，精英遭遇填 1")]
        [SerializeField] private int _relicCount;

        [SerializeField] private SO_RelicPool _relicPool;

        public int GoldMin => _goldMin;
        public int GoldMax => _goldMax;
        public int CardRewardCount => _cardRewardCount;
        public SO_CardPool CardPool => _cardPool;
        public float PotionChance => _potionChance;
        public SO_PotionPool PotionPool => _potionPool;
        public int RelicCount => _relicCount;
        public SO_RelicPool RelicPool => _relicPool;
    }
}
