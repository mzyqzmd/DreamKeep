namespace _DreamKeep.GameBattle
{
    /// <summary>玩家回合结束：进入玩家回合结束阶段（弃手牌与衰减）即离开操作窗口，视图据此落闸（手牌下沉置灰、不可拖拽），直到下个玩家回合开始。</summary>
    public struct BE_PlayerTurnEnded : IBattleEvent
    {
    }
}
