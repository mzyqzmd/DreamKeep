namespace _DreamKeep.RuntimeData
{
    /// <summary>玩家战斗态：进战斗时新建、战斗结束时清空，只装玩家侧的战斗数据（五个牌堆与能量）。
    /// 打出堆是"正在结算"区——卡牌打出后先落这里，结算完成再按结果离场。</summary>
    public class PlayerCombatState
    {
        public CardPile DrawPile { get; } = new();
        public CardPile Hand { get; } = new();
        public CardPile DiscardPile { get; } = new();
        public CardPile ExhaustPile { get; } = new();
        public CardPile PlayPile { get; } = new();

        public int CurrentEnergy { get; set; }
        public int MaxEnergy { get; }

        /// <summary>手牌上限：手牌达到该张数后抽到的牌不入手牌、直接进弃牌堆（洗回与抽牌堆消耗照常发生）。数值取自设计稿《04-完整单人卡牌组-守梦人》的基础参数，属待校准项。</summary>
        public const int MaxHandSize = 10;

        public PlayerCombatState(int maxEnergy_)
        {
            MaxEnergy = maxEnergy_;
            CurrentEnergy = maxEnergy_;
        }
    }
}
