namespace _DreamKeep.GameRun
{
    /// <summary>战斗退出收尾：无奖励配置、掷空或表现弹板失败的共同出口（开发期配置缺口，战败走 BAE_RunDefeated），
    /// 表现侧借此立即卸段（FinishSegment）。</summary>
    public struct BAE_BattleExited : IBattleSegmentEvent
    {
    }
}
