using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「壁垒」运行时实例：常驻标记，回合开始的清盾步骤据此跳过，不移除也不衰减。</summary>
    public sealed class BP_Barricade : BattlePerk
    {
        private SO_Barricade Config => (SO_Barricade)_data;

        public BP_Barricade(SO_Barricade data_) : base(data_) { }

        public override bool Settle() => false;

        public override string GetDescription() => "格挡不再在你的\n回合开始时消失";

        public override BattlePerk Clone() => WithLayers(new BP_Barricade(Config));
    }
}
