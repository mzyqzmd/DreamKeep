namespace _DreamKeep.GameBattle
{
    /// <summary>某方回合开始：携带当前回合序号与是否玩家回合，供表现层判断「现在轮到谁、第几回合」。</summary>
    public struct BE_TurnStarted : IBattleEvent
    {
        public int TurnCount { get; }
        public bool IsPlayerTurn { get; }

        public BE_TurnStarted(int turnCount_, bool isPlayerTurn_)
        {
            TurnCount = turnCount_;
            IsPlayerTurn = isPlayerTurn_;
        }
    }
}
