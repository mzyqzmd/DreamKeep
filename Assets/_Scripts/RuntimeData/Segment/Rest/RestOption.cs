using UnityEngine;

namespace _DreamKeep.RuntimeData
{
    /// <summary>单条休息选项：标题、说明、图标、是否可点（false＝置灰，悬停文案仍显示）。
    /// 动态选项（回复/烹饪）的数值在此带出，文案组装与染色/换行装饰归表现层——域不出富文本标记。</summary>
    public readonly struct RestOption
    {
        public string Title { get; }
        public string Description { get; }
        public Sprite Icon { get; }
        public bool Enabled { get; }

        /// <summary>回复选项：按最大生命百分比的回血比例（0.2 = 20%）；其余选项为 null。</summary>
        public float? HealRatio { get; }

        /// <summary>回复选项：按比例换算出的绝对回血量；其余选项为 null。</summary>
        public int? HealAmount { get; }

        /// <summary>烹饪选项：要删除的牌数；其余选项为 null。</summary>
        public int? RemoveCards { get; }

        /// <summary>烹饪选项：最大生命提升量；其余选项为 null。</summary>
        public int? HpGain { get; }

        public RestOption(string title_, string description_, Sprite icon_, bool enabled_,
            float? healRatio_ = null, int? healAmount_ = null, int? removeCards_ = null, int? hpGain_ = null)
        {
            Title = title_;
            Description = description_;
            Icon = icon_;
            Enabled = enabled_;
            HealRatio = healRatio_;
            HealAmount = healAmount_;
            RemoveCards = removeCards_;
            HpGain = hpGain_;
        }
    }
}
