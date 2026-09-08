namespace _DreamKeep.Gameplay
{
    /// <summary>回合阶段切换：阶段名由状态机给出，六阶段循环。</summary>
    public class TurnPhaseChangedEvent : IDomainEvent
    {
        public string PhaseName { get; }

        public TurnPhaseChangedEvent(string phaseName_) => PhaseName = phaseName_;
    }
}
