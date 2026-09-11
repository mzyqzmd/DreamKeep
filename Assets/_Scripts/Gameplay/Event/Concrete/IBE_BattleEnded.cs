namespace _DreamKeep.Gameplay
{
    /// <summary>战斗结束：胜或负由 IsVictory 区分。</summary>
    public struct IBE_BattleEnded : IBattleEvent
    {
        public bool IsVictory { get; }

        public IBE_BattleEnded(bool isVictory_) => IsVictory = isVictory_;
    }
}
