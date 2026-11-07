using System;
using System.Collections.Generic;

namespace _DreamKeep.Utility
{
    /// <summary>确定性随机源：以种子驱动，state 可导入导出，保证同一局的无头模拟与存档回放可复现。
    /// 存档拍平与还原都只改 state，不替换 Rng 实例（整局多处持有它的引用）。</summary>
    public class Rng
    {
        // xorshift32 的 state 恒不能为 0，否则序列卡死；构造与导入都兜到这个非零常量
        private const uint FallbackState = 0x9E3779B9u;

        private uint _state;

        public Rng(int seed_) => _state = seed_ == 0 ? FallbackState : unchecked((uint)seed_);

        /// <summary>随机流当前状态：写档时读出、读档时就地写回。</summary>
        public uint State
        {
            get => _state;
            set => _state = value == 0 ? FallbackState : value;
        }

        /// <summary>返回 [0, maxExclusive_) 区间内的随机整数。</summary>
        public int Next(int maxExclusive_) => maxExclusive_ <= 0 ? 0 : (int)(NextDouble() * maxExclusive_);

        /// <summary>返回 [minInclusive_, maxExclusive_) 区间内的随机整数。</summary>
        public int Next(int minInclusive_, int maxExclusive_) =>
            maxExclusive_ <= minInclusive_ ? minInclusive_ : minInclusive_ + (int)(NextDouble() * (maxExclusive_ - minInclusive_));

        /// <summary>返回 [minInclusive_, maxExclusive_) 区间内的随机浮点数。</summary>
        public float Next(float minInclusive_, float maxExclusive_) =>
            (float)(NextDouble() * (maxExclusive_ - minInclusive_) + minInclusive_);

        /// <summary>从序列中随机取一个元素，序列为空时返回默认值。</summary>
        public T Pick<T>(IReadOnlyList<T> items_) => items_.Count == 0 ? default : items_[Next(items_.Count)];

        // 每次推进一格状态；所有 Next / Pick 都只经它取数，随机流消耗次数对存档可复现
        private uint NextUInt()
        {
            uint x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            return x;
        }

        private double NextDouble() => NextUInt() / 4294967296.0;

        /// <summary>原位局部洗牌并取前 take_ 个：换牌循环与奖励掷骰处既有的"逐位换牌"逐次一致，随机流推进序列不变；
        /// take_ 超过表长按表长截断。</summary>
        public List<T> TakeShuffled<T>(List<T> list_, int take_)
        {
            int count = Math.Min(take_, list_.Count);
            for (int i = 0; i < count; i++)
            {
                int j = Next(i, list_.Count);
                (list_[i], list_[j]) = (list_[j], list_[i]);
            }

            return list_.GetRange(0, count);
        }

        /// <summary>原位全量洗牌：循环止于倒数第二位（末位自换无意义、不消耗随机流），与地图生成器既有实现逐次一致。</summary>
        public void Shuffle<T>(List<T> list_)
        {
            for (int i = 0; i < list_.Count - 1; i++)
            {
                int j = Next(i, list_.Count);
                (list_[i], list_[j]) = (list_[j], list_[i]);
            }
        }

        /// <summary>加权无放回抽样：按候选件权重轮盘逐件取出，take_ 超过表长按表长截断；
        /// 权重 ≤0 的件不会被抽中，全部权重 ≤0 时退化为等权抽取（零总和轮盘会卡死，此处对齐 TakeShuffled 口径）。</summary>
        public List<T> TakeWeighted<T>(IReadOnlyList<WeightedItem<T>> candidates_, int take_)
        {
            List<WeightedItem<T>> pool = new(candidates_.Count);
            for (int i = 0; i < candidates_.Count; i++) pool.Add(candidates_[i]);

            int count = Math.Min(take_, pool.Count);
            List<T> taken = new(count);
            for (int i = 0; i < count; i++)
            {
                int picked = PickWeightedIndex(pool, i);
                taken.Add(pool[picked].Item);
                (pool[i], pool[picked]) = (pool[picked], pool[i]);
            }

            return taken;
        }

        // 轮盘定位：[from_, end) 区间按权重掷一次返回命中下标；跳过零权重件，区间总权重 ≤0 时等权掷
        private int PickWeightedIndex<T>(List<WeightedItem<T>> pool_, int from_)
        {
            float total = 0f;
            for (int i = from_; i < pool_.Count; i++) total += pool_[i].Weight;

            if (total <= 0f) return Next(from_, pool_.Count);

            float roll = Next(0f, total);
            int lastPositive = -1;
            for (int i = from_; i < pool_.Count; i++)
            {
                if (pool_[i].Weight <= 0f) continue;
                lastPositive = i;
                roll -= pool_[i].Weight;
                if (roll < 0f) return i;
            }

            return lastPositive; // 浮点误差兜底：归到最后一件正权重件
        }
    }
}
