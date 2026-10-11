using System;
using System.Collections.Generic;
using _DreamKeep.Utility;

namespace _DreamKeep.RuntimeData
{
    /// <summary>运行时状态容器：卡进战斗克隆出的可变副本，持有游戏中会改变的数据（费用、升级态、词条）。
    /// 词条分两套烘焙集（未升级 / 已升级）与一套运行时追加集，生效集按升级态现算并缓存，升级只是切档。
    /// 不含任何静态配置；可变性守卫与词条操作均在此实现。克隆时仅深拷贝本容器，静态配置由 CardModel 共享引用。</summary>
    internal class CM_Runtime
    {
        internal string Name => IsUpgraded ? _name + " +" : _name;

        internal ModifiableStat Cost { get; }
        internal bool IsUpgraded { get; private set; }

        /// <summary>运行时追加的词条，跨升级档保留。</summary>
        internal List<CardAffix> AdditionalAffixes { get; } = new();

        /// <summary>当前生效的词条集：按升级态取一套烘焙词条，再并入运行时追加的词条。</summary>
        internal IReadOnlyList<CardAffix> ActiveAffixes => _activeAffixes;

        /// <summary>保留词条权重恒为最小且插入稳定，只要存在必在首位。</summary>
        internal bool IsRetain => _activeAffixes.Count > 0 && _activeAffixes[0] is CA_Retain;

        /// <summary>是否带"不能被打出"词条：全量扫描生效集，与词条位次无关。</summary>
        internal bool IsUnableToPlay => _activeAffixes.Exists(affix_ => affix_ is CA_UnableToPlay);

        private readonly string _name;
        /// <summary>是否为卡组原型：原型上除升级外的写操作一律抛异常。</summary>
        private readonly bool _isCanonical;
        private readonly List<CardAffix> _baseAffixes = new();
        private readonly List<CardAffix> _upgradedAffixes = new();
        private readonly List<CardAffix> _activeAffixes = new();

        internal CM_Runtime(string name_, int cost_, bool isCanonical_)
        {
            _name = name_;
            Cost = new ModifiableStat(cost_);
            _isCanonical = isCanonical_;
        }

        #region 烘焙期专用：写入两套词条并重建生效集，供配置烘焙调用

        internal void SetBakedAffixes(List<CardAffix> baseAffixes_, List<CardAffix> upgradedAffixes_)
        {
            _baseAffixes.Clear();
            _baseAffixes.AddRange(baseAffixes_);
            _upgradedAffixes.Clear();
            _upgradedAffixes.AddRange(upgradedAffixes_);
            RebuildActive();
        }

        #endregion

        #region 切换升级档：生效词条集换成已升级集，运行时追加的词条保留

        internal void Upgrade()
        {
            if (IsUpgraded || _upgradedAffixes.Count == 0) return;
            IsUpgraded = true;
            RebuildActive();
        }

        #endregion

        #region 克隆一份可变副本：两套词条与追加词条逐条深拷贝、费用取当前值、升级态保留、打散原型标记

        internal CM_Runtime Clone()
        {
            CM_Runtime copy = new(_name, Cost.Value, false);
            for (int i = 0; i < _baseAffixes.Count; i++)
                copy._baseAffixes.Add(_baseAffixes[i].Clone());
            for (int i = 0; i < _upgradedAffixes.Count; i++)
                copy._upgradedAffixes.Add(_upgradedAffixes[i].Clone());
            for (int i = 0; i < AdditionalAffixes.Count; i++)
                copy.AdditionalAffixes.Add(AdditionalAffixes[i].Clone());
            copy.IsUpgraded = IsUpgraded;
            copy.RebuildActive();
            return copy;
        }

        #endregion

        #region 追加一条词条：按管线权重有序插入追加集并重建生效集，原型上调用抛异常

        internal void AddAffix(CardAffix affix_)
        {
            if (_isCanonical) throw new InvalidOperationException("卡原型不可在运行时被改动，请先经 CardModel.ToMutable() 克隆出可变副本。");
            InsertOrdered(AdditionalAffixes, affix_);
            RebuildActive();
        }

        #endregion

        /// <summary>重建生效集：按升级态取一套烘焙词条，再有序并入追加词条。</summary>
        private void RebuildActive()
        {
            _activeAffixes.Clear();
            List<CardAffix> baked = IsUpgraded && _upgradedAffixes.Count > 0 ? _upgradedAffixes : _baseAffixes;
            for (int i = 0; i < baked.Count; i++)
                InsertOrdered(_activeAffixes, baked[i]);
            for (int i = 0; i < AdditionalAffixes.Count; i++)
                InsertOrdered(_activeAffixes, AdditionalAffixes[i]);
        }

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
