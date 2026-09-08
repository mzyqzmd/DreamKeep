using System.Collections.Generic;
using _DreamKeep.Data;
using _DreamKeep.Utility;

namespace _DreamKeep.RuntimeData
{
    /// <summary>局外卡：一局游戏内的持久卡，属性恒定、只有未升级与已升级两种形态，词条集合对外只读。</summary>
    public class InGameCard
    {
        private static int _nextInstanceId = 1;

        private readonly List<CardAffix> _affixes = new();
        private readonly bool _canUpgrade;

        /// <summary>本局内唯一实例 Id，同 CardId 可有多份，供升级/移除与存档定位。</summary>
        public int InstanceId { get; }
        /// <summary>配置卡 Id，供表现层反查名称/图标。</summary>
        public string CardId { get; }
        public bool IsUpgraded { get; private set; }
        public ModifiableStat Cost { get; }
        /// <summary>落点规则：由配置直接烘焙而来，运行时恒定。</summary>
        public ECardTargetType Target { get; }
        public IReadOnlyList<CardAffix> Affixes => _affixes;
        public bool CanUpgrade => _canUpgrade && !IsUpgraded;

        internal InGameCard(string cardId_, int cost_, ECardTargetType target_, bool canUpgrade_)
        {
            InstanceId = _nextInstanceId++;
            CardId = cardId_;
            Cost = new ModifiableStat(cost_);
            Target = target_;
            _canUpgrade = canUpgrade_;
        }

        /// <summary>获取卡牌描述。</summary>
        public string GetDescription() => CardAffixUtility.Describe(_affixes);

        #region 升级：就地重烘焙整份词条集合，实例 Id 保持不变
        public void Upgrade()
        {
            if (!CanUpgrade) return;
            IsUpgraded = true;
            CardBaker.Rebake(this);
        }
        #endregion

        internal void AddAffix(CardAffix affix_) => CardAffixUtility.InsertOrdered(_affixes, affix_);

        internal void ClearAffixes() => _affixes.Clear();
    }
}
