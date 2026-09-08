using System;

namespace _DreamKeep.Utility
{
    [Serializable]
    public struct StatModifier : IEquatable<StatModifier>
    {
        public float additive;
        public float multiplier;

        public StatModifier(float additive_, float multiplier_)
        {
            additive = additive_;
            multiplier = multiplier_;
        }

        public readonly bool Equals(StatModifier other)
        {
            if (additive == other.additive &&
                multiplier == other.multiplier)
                return true;
            return false;
        }
    }
}
