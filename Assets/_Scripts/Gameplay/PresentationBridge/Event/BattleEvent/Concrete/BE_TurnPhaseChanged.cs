namespace _DreamKeep.Gameplay
{
    /// <summary>回合阶段切换：阶段名由状态机给出，六阶段循环。</summary>
    public struct BE_TurnPhaseChanged : IBattleEvent
    {
        public string PhaseName { get; }

        public BE_TurnPhaseChanged(string phaseName_) => PhaseName = phaseName_;
    }
}
