using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「禁止抽牌」运行时实例：本回合内抽牌动作直接失效，回合结束结算时自动移除。</summary>
    public sealed class BP_NoDraw : BattlePerk
    {
        private SO_NoDraw Config => (SO_NoDraw)_data;

        public BP_NoDraw(SO_NoDraw data_) : base(data_) { }

        public override string GetDescription() => "本回合不能再抽牌";

        public override BattlePerk Clone() => WithLayers(new BP_NoDraw(Config));
    }
}
