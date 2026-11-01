using System.Collections.Generic;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>遭遇奖励配置：一场战斗打完给什么——金币区间、卡牌奖励组数（每组恒三选一）、药水掉落概率与遗物件数，以及三类抽取池列表。
    /// 池列表内所有池合并成一个候选集后等权抽取，想按稀有度/角色分池就多建几份池资产、在这里逐个列出。
    /// 战斗独有，由 <see cref="SO_Encounter"/> 引用；Boss 等终局遭遇不配（打完即结束），留空即不结算。</summary>
    [CreateAssetMenu(fileName = "Reward", menuName = "DreamKeep/Data/Encounter/Reward")]
    public sealed class SO_EncounterReward : ScriptableObject
    {
        [Header("金币")] [SerializeField] private int _goldMin = 15;
        [SerializeField] private int _goldMax = 25;

        [Header("卡牌奖励")] [Tooltip("给几组卡牌奖励：每组固定三选一")] [SerializeField]
        private int _cardRewardCount = 1;

        [Tooltip("候选卡池列表：所有池合并后等权抽取")] [SerializeField]
        private List<SO_CardPool> _cardPools = new();

        [Tooltip("抽到的这张卡是被升级的概率")] [Range(0f, 1f)] [SerializeField]
        private float _cardUpgradeChance = 0.1f;

        [Header("药水奖励")] [Range(0f, 1f)] [SerializeField]
        private float _potionChance = 0.3f;

        [Tooltip("候选药水池列表：所有池合并后等权抽取")] [SerializeField]
        private List<SO_PotionPool> _potionPools = new();

        [Header("遗物奖励")] [Tooltip("给几件遗物：普通遭遇填 0，精英遭遇填 1")] [SerializeField]
        private int _relicCount;

        [Tooltip("候选遗物池列表：所有池合并后等权抽取，已持有的会被跳过")] [SerializeField]
        private List<SO_RelicPool> _relicPools = new();

        public int GoldMin => _goldMin;
        public int GoldMax => _goldMax;
        public int CardRewardCount => _cardRewardCount;
        public float CardUpgradeChance => _cardUpgradeChance;
        public IReadOnlyList<SO_CardPool> CardPools => _cardPools;
        public float PotionChance => _potionChance;
        public IReadOnlyList<SO_PotionPool> PotionPools => _potionPools;
        public int RelicCount => _relicCount;
        public IReadOnlyList<SO_RelicPool> RelicPools => _relicPools;
    }
}