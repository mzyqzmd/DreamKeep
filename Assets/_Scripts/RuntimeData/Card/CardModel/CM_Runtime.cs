using System;
using System.Collections.Generic;
using _DreamKeep.Utility;

namespace _DreamKeep.RuntimeData
{
    /// <summary>运行时状态容器：卡进战斗克隆出的可变副本，持有游戏中会改变的数据（费用、升级态、词条）。
    /// 词条只有一套烘焙基础集与一套运行时追加集，升级靠烘焙期解析好的补丁现改数值/追加词条/移除词条；
    /// 不含任何静态配置；可变性守卫与词条操作均在此实现。克隆时仅深拷贝本容器，静态配置由 CardModel 共享引用。</summary>
    internal class CM_Runtime
    {
        internal string Name => IsUpgraded ? _name + " +" : _name;

        internal ModifiableStat Cost { get; }
        internal bool IsUpgraded { get; private set; }

        /// <summary>运行时追加的词条：升级补丁追加的新词条落在这里。</summary>
        internal List<CardAffix> AdditionalAffixes { get; } = new();

        /// <summary>当前生效的词条集：基础词条（升级后跳过被移除的）再并入追加词条。</summary>
        internal IReadOnlyList<CardAffix> ActiveAffixes => _activeAffixes;

        /// <summary>是否带"保留"词条：全量扫描生效集，与词条位次无关。</summary>
        internal bool IsRetain => _activeAffixes.Exists(affix_ => affix_ is CA_Retain);

        /// <summary>是否带"不能被打出"词条：全量扫描生效集，与词条位次无关。</summary>
        internal bool IsUnableToPlay => _activeAffixes.Exists(affix_ => affix_ is CA_UnableToPlay);

        private readonly string _name;
        /// <summary>是否为卡组原型：原型上除升级外的写操作一律抛异常。</summary>
        private readonly bool _isCanonical;
        private readonly List<CardAffix> _baseAffixes = new();
        private readonly List<CardAffix> _activeAffixes = new();
        private readonly List<CM_ValuePatch> _valuePatches = new();        // 升级补丁·改数值（烘焙期解析）
        private readonly List<CardAffix> _upgradeAddedAffixes = new();     // 升级补丁·追加词条（烘焙期解析）
        private readonly List<int> _upgradeRemovedIndexes = new();         // 升级补丁·移除词条：基础词条下标

        internal CM_Runtime(string name_, int cost_, bool isCanonical_)
            : this(name_, new ModifiableStat(cost_), isCanonical_)
        {
        }

        private CM_Runtime(string name_, ModifiableStat cost_, bool isCanonical_)
        {
            _name = name_;
            Cost = cost_;
            _isCanonical = isCanonical_;
        }

        #region 烘焙期专用：写入基础词条与升级补丁并重建生效集，供配置烘焙调用

        internal void SetBakedContent(List<CardAffix> baseAffixes_, List<CM_ValuePatch> valuePatches_,
            List<CardAffix> upgradeAddedAffixes_, List<int> upgradeRemovedIndexes_)
        {
            _baseAffixes.Clear();
            _baseAffixes.AddRange(baseAffixes_);
            _valuePatches.Clear();
            _valuePatches.AddRange(valuePatches_);
            _upgradeAddedAffixes.Clear();
            _upgradeAddedAffixes.AddRange(upgradeAddedAffixes_);
            _upgradeRemovedIndexes.Clear();
            _upgradeRemovedIndexes.AddRange(upgradeRemovedIndexes_);
            RebuildActive();
        }

        #endregion

        #region 升级：应用补丁现改数值与词条集，一次性且不可逆；原型上调用抛异常

        internal void Upgrade()
        {
            if (_isCanonical) throw new InvalidOperationException("卡原型不可在运行时被改动，请先经 CardModel.ToMutable() 克隆出可变副本。");
            if (IsUpgraded) return;

            IsUpgraded = true;
            for (int i = 0; i < _valuePatches.Count; i++)
                ApplyPatch(_valuePatches[i]);
            for (int i = 0; i < _upgradeAddedAffixes.Count; i++)
                InsertOrdered(AdditionalAffixes, _upgradeAddedAffixes[i].Clone());
            RebuildActive();
        }

        #endregion

        #region 克隆一份可变副本：值与移除标记随克隆，补丁只随"未升级"那份

        internal CM_Runtime Clone()
        {
            CM_Runtime copy = new(_name, Cost.Clone(), false);
            for (int i = 0; i < _baseAffixes.Count; i++)
                copy._baseAffixes.Add(_baseAffixes[i].Clone());
            for (int i = 0; i < AdditionalAffixes.Count; i++)
                copy.AdditionalAffixes.Add(AdditionalAffixes[i].Clone());
            copy._upgradeRemovedIndexes.AddRange(_upgradeRemovedIndexes);
            copy.IsUpgraded = IsUpgraded;

            // 未升级副本带着补丁（之后自己升级的结果与源一致）；已升级副本不带，否则会在已含修正的值上再施加一遍
            if (!IsUpgraded)
            {
                copy._valuePatches.AddRange(_valuePatches);
                for (int i = 0; i < _upgradeAddedAffixes.Count; i++)
                    copy._upgradeAddedAffixes.Add(_upgradeAddedAffixes[i].Clone());
            }

            copy.RebuildActive();
            return copy;
        }

        #endregion

        // 应用一条升级补丁：层数补丁加在状态原型上，卡级费用直接加在容器上，其余按通道找到数值容器
        private void ApplyPatch(CM_ValuePatch patch_)
        {
            if (patch_.Kind == null)
            {
                ((CA_ApplyPerk)_baseAffixes[patch_.AffixIndex]).Perk.AddLayers((int)patch_.Modifier.additive);
                return;
            }

            if (patch_.AffixIndex < 0)
            {
                Cost.Add(patch_.Modifier);
                return;
            }

            CM_ValuePatch.StatOf(patch_.Kind.Value, _baseAffixes[patch_.AffixIndex]).Add(patch_.Modifier);
        }

        /// <summary>重建生效集：基础词条（升级后跳过被移除的下标）与追加词条分别有序并入。</summary>
        private void RebuildActive()
        {
            _activeAffixes.Clear();
            for (int i = 0; i < _baseAffixes.Count; i++)
            {
                if (IsUpgraded && _upgradeRemovedIndexes.Contains(i)) continue;
                InsertOrdered(_activeAffixes, _baseAffixes[i]);
            }

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
