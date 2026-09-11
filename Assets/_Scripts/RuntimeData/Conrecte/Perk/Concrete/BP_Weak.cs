using _DreamKeep.Data;
using UnityEngine;

namespace _DreamKeep.RuntimeData
{
    /// <summary>虚弱：每层降低造成的伤害，回合结束层数递减。</summary>
    public class BP_Weak : BattlePerk
    {
        private readonly PD_Weak _weak;
        internal BP_Weak(int layers_, PD_Weak weak_) : base(layers_) => _weak = weak_;
        public override string Name => _weak.Name;
        public float Ratio => _weak.Ratio;
        public override Sprite Icon => _weak.Icon;
        public override string GetDescription() => $"从卡牌中造成的伤害减少 {Ratio * 100} %";
        public override BattlePerk Clone() => new BP_Weak(Layers, _weak);
    }
}
