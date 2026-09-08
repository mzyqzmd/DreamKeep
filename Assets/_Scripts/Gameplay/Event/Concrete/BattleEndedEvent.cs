namespace _DreamKeep.Gameplay
{
    /// <summary>战斗结束：胜或负由 IsVictory 区分。</summary>
    public class BattleEndedEvent : IDomainEvent
    {
        public bool IsVictory { get; }

        public BattleEndedEvent(bool isVictory_) => IsVictory = isVictory_;
    }
}
