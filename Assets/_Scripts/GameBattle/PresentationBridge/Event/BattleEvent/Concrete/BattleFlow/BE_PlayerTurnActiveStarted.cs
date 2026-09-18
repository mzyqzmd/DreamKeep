namespace _DreamKeep.GameBattle
{
    /// <summary>玩家回合进行阶段开始：唯一允许玩家操作（打牌 / 结束回合）的窗口，视图据此开闸，直到结束回合命令被处理或战斗结束。</summary>
    public struct BE_PlayerTurnActiveStarted : IBattleEvent
    {
    }
}
