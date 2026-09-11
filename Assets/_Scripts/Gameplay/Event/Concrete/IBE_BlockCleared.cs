namespace _DreamKeep.Gameplay
{
    /// <summary>格挡清空：记录被清空的目标，供举盾待机态退回普通待机。</summary>
    public struct IBE_BlockCleared : IBattleEvent
    {
        public Actor Target { get; }

        public IBE_BlockCleared(Actor target_) => Target = target_;
    }
}
