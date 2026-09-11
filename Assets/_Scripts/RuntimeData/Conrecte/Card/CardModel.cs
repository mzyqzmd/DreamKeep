using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using _DreamKeep.Data;
using _DreamKeep.Utility;
using Sirenix.Utilities;
using UnityEngine;

namespace _DreamKeep.RuntimeData
{
    /// <summary>运行时卡：局外持久卡与战斗内卡共用的唯一形态。canonical 为卡组原型（除升级外不可变），mutable 为进战斗克隆出的可变副本。卡的身份即对象引用，刻意不覆写 Equals 与 GetHashCode，按卡 Id 覆写值相等会让引用型字典与映射静默错乱；卡组编号不属于本类，由卡组服务管理。</summary>
    public class CardModel
    {
        //该Id用于从数据库中找卡牌配置数据
        public string CardId { get; }
        public string Name { get; private set; }
        public ModifiableStat Cost { get; }

        private readonly bool _canUpgrade;

        /// <summary>是否为卡组原型：原型上除升级外的写操作一律抛异常。</summary>
        private bool _isCanonical;

        public List<CardAffix> AdditionalAffixes{ get; } = new();
        public List<CardAffix> ActiveAffixes { get; } = new();
        public bool IsUpgraded { get; private set; }

        /// <summary>保留词条权重恒为最小且插入稳定，只要存在必在首位。</summary>
        public bool IsRetain => ActiveAffixes.Count > 0 && ActiveAffixes[0] is CA_Retain;

        internal CardModel(string cardId_, string cardName_, int cost_, bool canUpgrade_)
        {
            CardId = cardId_;
            Name = cardName_;
            Cost = new ModifiableStat(cost_);
            _canUpgrade = canUpgrade_;
            _isCanonical = true;
        }

        #region 获取卡牌描述

        public string GetDescription()
        {
            StringBuilder description = new();
            for (int i = 0; i < ActiveAffixes.Count; i++)
                description.AppendLine(ActiveAffixes[i].GetDescription());
            return description.ToString();
        }

        #endregion

        #region 克隆一份可变副本：进战斗时由卡组原型产出，词条逐条深拷贝、费用取当前值

        public CardModel ToMutable()
        {
            CardModel copy = new(CardId, Name, Cost.Value, _canUpgrade)
                { _isCanonical = false, IsUpgraded = IsUpgraded };
            for (var i = 0; i < ActiveAffixes.Count; i++)
            {
                copy.AdditionalAffixes.Add(AdditionalAffixes[i].Clone());
                copy.ActiveAffixes.Add(ActiveAffixes[i].Clone());
            }

            return copy;
        }

        #endregion

        #region 追加一条词条：按管线权重有序插入，原型上调用抛异常

        public void AddAffix(CardAffix affix_)
        {
            if (_isCanonical) throw new InvalidOperationException($"卡 {Name} 是卡组原型，不可在运行时被改动，请先 ToMutable() 克隆出可变副本。");
            InsertOrdered(AdditionalAffixes, affix_);
            InsertOrdered(ActiveAffixes, affix_);
        }

        #endregion

        #region 升级

        public void Upgrade()
        {
            if (!_canUpgrade || IsUpgraded) return;
            IsUpgraded = true;
            Name += " +";
            CardBaker.Rebake(this);
        }

        #endregion

        #region 烘焙期专用

        /// <summary>烘焙期专用：免断言追加词条，供配置烘焙与升级重烘焙调用。</summary>
        internal void AddAffixUnchecked(CardAffix affix_) =>
            InsertOrdered(ActiveAffixes, affix_);

        /// <summary>烘焙期专用：清空全部词条，供升级重烘焙调用。</summary>
        internal void ClearAffixes() => ActiveAffixes.Clear();

        #endregion

        private static void InsertOrdered(List<CardAffix> affixes_, CardAffix affix_)
        {
            int index = affixes_.Count;
            for (int i = 0; i < affixes_.Count; i++)
            {
                if (affixes_[i].PipelineOrder <= affix_.PipelineOrder) continue;
                index = i;
                break;
            }

            affixes_.Insert(index, affix_);
        }
    }
}