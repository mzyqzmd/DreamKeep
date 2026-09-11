namespace _DreamKeep.Gameplay
{
    /// <summary>回合阶段切换：阶段名由状态机给出，六阶段循环。</summary>
    public struct IBE_TurnPhaseChanged : IBattleEvent
    {
        public string PhaseName { get; }

        public IBE_TurnPhaseChanged(string phaseName_) => PhaseName = phaseName_;
    }
}
