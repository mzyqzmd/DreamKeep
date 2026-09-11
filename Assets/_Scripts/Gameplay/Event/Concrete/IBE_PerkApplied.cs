namespace _DreamKeep.Gameplay
{
    /// <summary>状态施加：记录目标、状态名与施加后的总层数。</summary>
    public struct IBE_PerkApplied : IBattleEvent
    {
        public Actor Target { get; }
        public string PerkName { get; }
        public int Layers { get; }

        public IBE_PerkApplied(Actor target_, string perkName_, int layers_)
        {
            Target = target_;
            PerkName = perkName_;
            Layers = layers_;
        }
    }
}
