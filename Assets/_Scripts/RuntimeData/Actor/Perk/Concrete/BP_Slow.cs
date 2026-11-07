using _DreamKeep.ConfigData;
using _DreamKeep.Utility;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「缓慢」运行时实例：层数即增伤百分点，玩家每打出一张牌累积，宿主回合结束清零且常驻；
    /// 刻度在烘焙时从定义资产拷进本实例，之后流程若要改值改的是实例。</summary>
    public sealed class BP_Slow : BattlePerk
    {
        private readonly int _percentPerCard;

        private SO_Slow Config => (SO_Slow)_data;

        public BP_Slow(SO_Slow data_) : base(data_) => _percentPerCard = data_.PercentPerCard;

        private BP_Slow(SO_Slow data_, int percentPerCard_) : base(data_) => _percentPerCard = percentPerCard_;

        /// <summary>乘性贡献：宿主自己挨打时提高为 ×(1+Layers/100)。</summary>
        public StatModifier GetModifier(EStatChannel channel_, Creature host_, Creature source_, Creature target_) =>
            channel_ == EStatChannel.Damage && host_ == target_ ? new StatModifier(0, 1f + Layers / 100f) : StatModifier.Neutral;

        public override void OnCardPlayed() => AddLayers(_percentPerCard);

        public override bool Settle()
        {
            AddLayers(-Layers);
            return false;
        }

        public override string GetDescription() =>
            $"每打出一张牌，受到的伤害+ {_percentPerCard} %\n当前 + {Layers} %，回合结束清零";

        public override BattlePerk Clone() => WithLayers(new BP_Slow(Config, _percentPerCard));
    }
}
