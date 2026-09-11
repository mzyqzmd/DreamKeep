using _DreamKeep.Data;
using UnityEngine;

namespace _DreamKeep.RuntimeData
{
    /// <summary>再生：回合结束恢复等于层数的血量，随后层数自减 1。</summary>
    public class BP_Regen : BattlePerk
    {
        private readonly PD_Regen _regen;
        internal BP_Regen(int layers_, PD_Regen regen_) : base(layers_) => _regen = regen_;
        public override string Name => _regen.Name;
        public override Sprite Icon => _regen.Icon;
        public override string GetDescription() => $"回合结束恢复 {Layers} 点生命值，随后层数减少一层";
        public override BattlePerk Clone() => new BP_Regen(Layers, _regen);
    }
}
