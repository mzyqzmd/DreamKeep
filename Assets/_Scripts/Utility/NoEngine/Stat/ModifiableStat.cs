using System.Collections.Generic;

namespace _DreamKeep.Utility
{
    /// <summary>可修正数值：持有基础值与生效中的修正列表，当前值 = (基础值 + Σ加值) 逐条乘算后的整数四舍五入结果；
    /// 修正随克隆一并带走（升级这类永久改动必须活着进战斗）；变更一律经动作系统。</summary>
    public class ModifiableStat
    {
        private readonly int _baseValue;
        private List<StatModifier> _modifiers; // 懒分配：带不满修正的数值不白占一个空列表

        public ModifiableStat(int baseValue_) => _baseValue = baseValue_;

        /// <summary>修正后的当前值。</summary>
        public int Value
        {
            get
            {
                if (_modifiers == null) return _baseValue;

                float additive = 0;
                float multiplier = 1f;
                foreach (StatModifier modifier in _modifiers)
                {
                    additive += modifier.additive;
                    multiplier *= modifier.multiplier;
                }

                return (int)((_baseValue + additive) * multiplier);
            }
        }

        public void Add(StatModifier modifier_)
        {
            _modifiers ??= new List<StatModifier>();
            _modifiers.Add(modifier_);
        }

        #region 克隆一份独立副本：基础值与全部生效中的修正一并带走

        public ModifiableStat Clone()
        {
            ModifiableStat copy = new(_baseValue);
            if (_modifiers != null) copy._modifiers = new List<StatModifier>(_modifiers);
            return copy;
        }

        #endregion
    }
}
