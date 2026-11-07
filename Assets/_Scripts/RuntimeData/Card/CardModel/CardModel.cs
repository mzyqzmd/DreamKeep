using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.Utility;
using UnityEngine;

namespace _DreamKeep.RuntimeData
{
    /// <summary>运行时卡统一门面：融合卡面静态配置（CM_Static）与运行时状态（CM_Runtime）。
    /// 对外是唯一契约，UI 与战斗逻辑仅经本类取数，不感知数据来自配置还是运行时；
    /// 卡的身份即对象引用，刻意不覆写 Equals 与 GetHashCode，按引用比较可避免引用型字典与映射静默错乱。</summary>
    public class CardModel
    {
        private readonly CM_Static _static;
        private readonly CM_Runtime _runtime;

        public string Name => _runtime.Name;
        /// <summary>烘焙这份卡的配置来源：存档拍平时靠它换算成稳定 GUID，逻辑侧不回读配置数值。</summary>
        public SO_Card Source => _static.Source;
        public Sprite Icon => _static.Icon;
        public ECardType Type => _static.Type;
        public ECardRarity Rarity => _static.Rarity;

        #region 目标类型

        public bool IsNoneTarget => _static.IsNoneTarget;
        public bool IsSelfTarget => _static.IsSelfTarget;
        public bool IsFrontEnemyTarget => _static.IsFrontEnemyTarget;
        public bool IsAnyEnemyTarget => _static.IsAnyEnemyTarget;
        public bool IsRandomEnemyTarget => _static.IsRandomEnemyTarget;
        public bool IsAllEnemyTarget => _static.IsAllEnemyTarget;

        #endregion

        public ModifiableStat Cost => _runtime.Cost;
        public bool IsUpgraded => _runtime.IsUpgraded;

        /// <summary>还可否升级：配置带升级条目且尚未升级过——卡组预览开关的显隐判据。</summary>
        public bool CanUpgrade => _static.CanUpgrade && !IsUpgraded;

        /// <summary>未升级的基础形态（与静态配置共享的烘焙原型）：升级预览取消勾选时展示它——只读展示，禁止改动。</summary>
        public CardModel BaseVersion => _static.Base;
        public IReadOnlyList<CardAffix> ActiveAffixes => _runtime.ActiveAffixes;
        public bool IsRetain => _runtime.IsRetain;
        public bool IsUnableToPlay => _runtime.IsUnableToPlay;

        internal CardModel(CM_Static static_, CM_Runtime runtime_)
        {
            _static = static_;
            _runtime = runtime_;
        }

        #region 克隆一份可变副本：共享静态配置引用，仅深拷贝运行时状态

        public CardModel ToMutable() => new(_static, _runtime.Clone());

        #endregion

        public string GetTypeDescription() => _static.GetTypeDescription();

        #region 卡面文案：逐行出口与纯文本描述

        /// <summary>卡面逐行文案：目标提示行与普通词条行的 Affix 为 null，不展示描述的词条那一行 Text 即词条名并带出该词条——
        /// 卡面的关键词行样式与词条说明气泡都只认这一份判据，行序即卡面从上到下的顺序；数值型词条的数值经渲染器着色，传 null 即纯文本描述。</summary>
        public IReadOnlyList<(string Text, CardAffix Affix)> GetDescriptionLines(IStatRenderer valueRenderer_ = null)
        {
            List<(string Text, CardAffix Affix)> lines = new();
            string targetLine = _static.TargetType switch
            {
                ECardTargetType.AllEnemy => "对全体目标:",
                ECardTargetType.RandomEnemy => "对随机目标:",
                ECardTargetType.AnyEnemy => "指定目标:",
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(targetLine)) lines.Add((targetLine, null));
            for (int i = 0; i < ActiveAffixes.Count; i++)
            {
                CardAffix affix = ActiveAffixes[i];
                lines.Add(affix.ShowDescription ? (affix.GetDescription(valueRenderer_), null) : (affix.Name, affix));
            }

            return lines;
        }

        #endregion

        #region 升级：把配置里的升级补丁应用到基础词条上，纯运行时操作不回查配置

        public void Upgrade()
        {
            if (!_static.CanUpgrade) return;
            _runtime.Upgrade();
        }

        #endregion
    }
}