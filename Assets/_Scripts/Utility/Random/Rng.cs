using System;
using System.Collections.Generic;

namespace _DreamKeep.Utility
{
    /// <summary>确定性随机源：以种子驱动，保证同一局的无头模拟与存档回放可复现。</summary>
    public class Rng
    {
        private readonly Random _random;

        public Rng(int seed_) => _random = new Random(seed_);
        /// <summary>返回 [0, maxExclusive_) 区间内的随机整数。</summary>
        public int Next(int maxExclusive_) => _random.Next(maxExclusive_);

        /// <summary>返回 [minInclusive_, maxExclusive_) 区间内的随机整数。</summary>
        public int Next(int minInclusive_, int maxExclusive_) => _random.Next(minInclusive_, maxExclusive_);

        /// <summary>返回 [minInclusive_, maxExclusive_) 区间内的随机浮点数。</summary>
        public float Next(float minInclusive_, float maxExclusive_) =>
            (float)(_random.NextDouble() * (maxExclusive_ - minInclusive_) + minInclusive_);

        /// <summary>按整数百分比判定是否命中；全程 int 运算以保持跨环境一致。</summary>
        public bool Chance(int percent_) => _random.Next(100) < percent_;

        /// <summary>从序列中随机取一个元素，序列为空时返回默认值。</summary>
        public T Pick<T>(IReadOnlyList<T> items_) => items_.Count == 0 ? default : items_[_random.Next(items_.Count)];

    }
}
