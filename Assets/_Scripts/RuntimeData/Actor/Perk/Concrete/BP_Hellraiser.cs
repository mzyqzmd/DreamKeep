using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「地狱狂徒」运行时实例：常驻标记，抽到名字含「打击」的牌时由抽牌入口代为自动打出。</summary>
    public sealed class BP_Hellraiser : BattlePerk
    {
        private SO_Hellraiser Config => (SO_Hellraiser)_data;

        public BP_Hellraiser(SO_Hellraiser data_) : base(data_) { }

        /// <summary>是否应当自动打出这张牌：牌名含「打击」即为触发对象。</summary>
        public static bool ShouldAutoPlay(string cardName_) => cardName_.Contains("打击");

        public override string GetDescription() => "每当你抽到名字中有\n\"打击\"的牌时，对一名\n随机敌人打出这张牌";

        public override BattlePerk Clone() => WithLayers(new BP_Hellraiser(Config));
    }
}
