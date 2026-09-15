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

        public PlayerCombatState(int maxEnergy_)
        {
            MaxEnergy = maxEnergy_;
            CurrentEnergy = maxEnergy_;
        }
    }
}
