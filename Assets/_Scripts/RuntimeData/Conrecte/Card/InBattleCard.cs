using System.Collections.Generic;
using _DreamKeep.Data;
using _DreamKeep.Utility;

namespace _DreamKeep.RuntimeData
{
    /// <summary>战斗卡：每场战斗由局外卡深拷贝而来，词条集合与数值可在战斗内被改动，战斗结束即丢弃。</summary>
    public class InBattleCard
    {
        private static int _nextInstanceId = 1;

        private readonly List<CardAffix> _affixes = new();

        /// <summary>效果型词条，顺序即结算管线顺序。</summary>
        public List<CardAffix> Affixes => _affixes;
        /// <summary>本场战斗内唯一实例 Id，供命令与事件引用（CardId 相同可有多张）。</summary>
        public int InstanceId { get; }
        /// <summary>配置卡 Id，供表现层反查名称/图标/描述。</summary>
        public string CardId { get; }
        public ModifiableStat Cost { get; }
        /// <summary>落点规则：由局外卡复制而来，本场战斗内恒定。</summary>
        public ECardTargetType Target { get; }
        /// <summary>保留词条权重恒为最小且插入稳定，只要存在必在首位。</summary>
        public bool IsRetain => _affixes.Count > 0 && _affixes[0] is CA_Retain;

        /// <summary>需要玩家指定落点，未指定时回退到最前敌人。</summary>
        public bool NeedsAim => Target is ECardTargetType.AnyEnemy or ECardTargetType.AnyEnenmyAndBackEnemy;
        /// <summary>作用于全体存活敌人。</summary>
        public bool IsAllEnemies => Target is ECardTargetType.AllEnemy;
        /// <summary>随机选取一个存活敌人。</summary>
        public bool IsRandom => Target is ECardTargetType.RandomEnemy;
        /// <summary>作用于自己而非敌方。</summary>
        public bool IsSelf => Target is ECardTargetType.Player;
        /// <summary>从指定槽位起贯穿到最后一名存活敌人。</summary>
        public bool PiercesBack => Target is ECardTargetType.AnyEnenmyAndBackEnemy;

        internal InBattleCard(string cardId_, int cost_, ECardTargetType target_)
        {
            InstanceId = _nextInstanceId++;
            CardId = cardId_;
            Cost = new ModifiableStat(cost_);
            Target = target_;
        }

        /// <summary>获取卡牌描述。</summary>
        public string GetDescription() => CardAffixUtility.Describe(_affixes);

        #region 添加效果型词条，按管线权重有序插入
        public void AddAffix(CardAffix affix_) => CardAffixUtility.InsertOrdered(_affixes, affix_);
        #endregion

        #region 移除效果型词条
        public bool RemoveAffix(CardAffix affix_) => _affixes.Remove(affix_);
        #endregion
    }
}
