using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「紧勒」运行时实例：层数即名字含「防御」的牌额外获得的格挡值，战斗内不衰减。</summary>
    public sealed class BP_Fasten : BattlePerk
    {
        private SO_Fasten Config => (SO_Fasten)_data;

        public BP_Fasten(SO_Fasten data_) : base(data_)
        {
        }

        /// <summary>格挡加成：牌名含「防御」时返回层数，否则为 0。</summary>
        public int GetBonus(string cardName_) => cardName_.Contains("防御") ? Layers : 0;

        public override string GetDescription() => GetDescription(null, Layers);

        public override string GetDescription(IStatRenderer valueRenderer_, int baseLayers_) =>
            $"名字含\"防御\"的牌额外\n获得 {RenderLayers(valueRenderer_, baseLayers_)} 点格挡";

        public override BattlePerk Clone() => WithLayers(new BP_Fasten(Config));
    }
}