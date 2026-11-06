namespace _DreamKeep.GameRun
{
    /// <summary>整局战败：玩家真死亡（复活拦截者耗尽后生命归零，见 IDeathGuard 链），表现侧借此弹整局结果面板，
    /// 确认后走 ReturnToMenu 回主菜单重开；与 BAE_RunCleared 对仗，区别于无奖励胜利的退出收尾 BAE_BattleExited。</summary>
    public struct BAE_RunDefeated : IBattleSegmentEvent
    {
    }
}
