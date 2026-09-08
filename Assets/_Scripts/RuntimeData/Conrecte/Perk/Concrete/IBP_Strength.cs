using _DreamKeep.Data;
using UnityEngine;

namespace _DreamKeep.RuntimeData
{
    /// <summary>力量：每层提高 1 点攻击伤害，战斗内不自动衰减。</summary>
    public class IBP_Strength : InBattlePerk
    {
        private readonly PD_Strength _strength;
        internal IBP_Strength(int layers_, PD_Strength strength_) : base(layers_) => _strength = strength_;
        public override string Name => _strength.Name;
        public override bool DecaysEachTurn => false;
        public override Sprite Icon => _strength.Icon;
        public override string GetDescription() => $"从卡牌中造成的伤害提高 {Layers} 点";
        public override InBattlePerk Clone() => new IBP_Strength(Layers, _strength);
    }
}
