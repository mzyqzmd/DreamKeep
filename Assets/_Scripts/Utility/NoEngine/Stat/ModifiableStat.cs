using System.Collections.Generic;

namespace _DreamKeep.Utility
{
    /// <summary>可修正数值：持有基础值与生效中的修正列表，当前值 = (基础值 + Σ加值) 逐条乘算后取整，
    /// 再按各修正给出的上下限折叠（下限取最大、上限取最小）后截断；
    /// 修正随克隆一并带走（升级这类永久改动必须活着进战斗）；变更一律经动作系统；
    /// 临时求值可经对象池复用实例（Reset 复位后重新 Add）。</summary>
    public class ModifiableStat
    {
        private int _baseValue;
        private List<StatModifier> _modifiers; // 懒分配：带不满修正的数值不白占一个空列表

        public ModifiableStat(int baseValue_) => _baseValue = baseValue_;

        /// <summary>修正前的基准值（配置里的原始数值）。</summary>
        public int BaseValue => _baseValue;

        /// <summary>修正后的当前值。</summary>
        public int Value
        {
            get
            {
                if (_modifiers == null) return _baseValue;

                int additive = 0;
                float multiplier = 1f;
                int floor = int.MinValue;
                int cap = int.MaxValue;
                foreach (StatModifier modifier in _modifiers)
                {
                    additive += modifier.additive;
                    multiplier *= modifier.multiplier;
                    if (modifier.floor > floor) floor = modifier.floor;
                    if (modifier.cap < cap) cap = modifier.cap;
                }

                int value = (int)((_baseValue + additive) * multiplier);
                if (value > cap) value = cap;
                if (value < floor) value = floor;
                return value;
            }
        }

        public void Add(StatModifier modifier_)
        {
            _modifiers ??= new List<StatModifier>();
            _modifiers.Add(modifier_);
        }

        #region 复位成一次全新求值：供对象池复用实例

        /// <summary>改写基础值并清空修正列表；列表只 Clear 不置空，容量留给下一次复用（池化求值稳态零分配）。</summary>
        public void Reset(int baseValue_)
        {
            _baseValue = baseValue_;
            _modifiers?.Clear();
        }

        #endregion

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
