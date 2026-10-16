using System;

namespace _DreamKeep.Utility
{
    /// <summary>一条数值修正：加值与乘率，作用顺序为先加后乘。</summary>
    [Serializable]
    public struct StatModifier
    {
        public float additive;
        public float multiplier;

        public StatModifier(float additive_, float multiplier_)
        {
            additive = additive_;
            multiplier = multiplier_;
        }
    }
}
