using _DreamKeep.Data;
using UnityEngine;

namespace _DreamKeep.RuntimeData
{
    /// <summary>易伤：每层提高受到的伤害，回合结束层数递减。</summary>
    public class BP_Vulnerable : BattlePerk
    {
        private readonly PD_Vulnerable _vulnerable;
        internal BP_Vulnerable(int layers_, PD_Vulnerable vulnerable_) : base(layers_) => _vulnerable = vulnerable_;
        public override string Name => _vulnerable.Name;
        public float Ratio => _vulnerable.Ratio;
        public override Sprite Icon => _vulnerable.Icon;
        public override string GetDescription()=> $"受到的伤害提高 {Ratio * 100} %";
       
        public override BattlePerk Clone() => new BP_Vulnerable(Layers, _vulnerable);
    }
}
