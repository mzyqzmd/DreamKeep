namespace _DreamKeep.GameBattle
{
    /// <summary>战斗结束：胜或负由 IsVictory 区分。</summary>
    public struct BE_BattleEnded : IBattleEvent
    {
        public bool IsVictory { get; }

        public BE_BattleEnded(bool isVictory_) => IsVictory = isVictory_;
    }
}
