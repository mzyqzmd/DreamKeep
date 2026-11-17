using System.Text;
using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「薪火之源」运行时实例：层数即宿主回合开始获得的能量，常驻不衰减。</summary>
    public sealed class BP_Pyre : BattlePerk
    {
        private SO_Pyre Config => (SO_Pyre)_data;

        public BP_Pyre(SO_Pyre data_) : base(data_)
        {
        }

        public override int EnergyCapBonus => Layers;
        public override string GetDescription() => GetDescription(null, Layers);

        public override string GetDescription(IStatRenderer valueRenderer_, int baseLayers_)
        {
            StringBuilder sb = new("在你的回合开始时，\n获得 ");
            for (int i = 0; i < Layers; i++)
                sb.Append("<sprite name=\"energyIcon\">");
            sb.Append(" 。");
            return sb.ToString();
        }

        public override BattlePerk Clone() => WithLayers(new BP_Pyre(Config));
    }
}