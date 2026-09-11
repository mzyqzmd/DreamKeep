using _DreamKeep.Data;
using UnityEngine;

namespace _DreamKeep.RuntimeData
{
    /// <summary>灼烧：回合结束时受到等于层数的伤害，随后层数递减。</summary>
    public class BP_Burn : BattlePerk
    {
        private readonly PD_Burn _burn;
        internal BP_Burn(int layers_, PD_Burn burn_) : base(layers_) => _burn = burn_;
        public override string Name => _burn.Name;
        public override Sprite Icon => _burn.Icon;
        public override string GetDescription() => $"回合结束受到 {Layers} 点伤害，随后层数减少一层";
        public override BattlePerk Clone() => new BP_Burn(Layers, _burn);
    }
}
