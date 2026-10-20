using System;

namespace _DreamKeep.Utility
{
    /// <summary>一条数值修正：加值、乘率与上下限，作用顺序为先加后乘、最后 clamp 到上下限。
    /// 上下限由各修正给出后折叠（下限取最大、上限取最小）；默认极值即不参与截断。</summary>
    public struct StatModifier
    {
        public int additive;
        public float multiplier;
        public int floor;
        public int cap;

        /// <summary>中性修正：不改变任何值。禁止用 default 代替——struct 的 cap 默认是 0，会把值钳成 0。</summary>
        public static StatModifier Neutral => new(0);

        /// <summary>是否不改变任何值。</summary>
        public bool IsNeutral => additive == 0 && Math.Abs(multiplier - 1f) < float.Epsilon && floor == int.MinValue &&
                                 cap == int.MaxValue;

        public StatModifier(int additive_, float multiplier_ = 1f, int floor_ = int.MinValue, int cap_ = int.MaxValue)
        {
            additive = additive_;
            multiplier = multiplier_;
            floor = floor_;
            cap = cap_;
        }
    }
}