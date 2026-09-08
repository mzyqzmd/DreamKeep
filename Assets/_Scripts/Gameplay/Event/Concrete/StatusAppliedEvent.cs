namespace _DreamKeep.Gameplay
{
    /// <summary>状态施加：记录目标、状态名与施加后的总层数。</summary>
    public class StatusAppliedEvent : IDomainEvent
    {
        public Actor Target { get; }
        public string StatusName { get; }
        public int Layers { get; }

        public StatusAppliedEvent(Actor target_, string statusName_, int layers_)
        {
            Target = target_;
            StatusName = statusName_;
            Layers = layers_;
        }
    }
}
