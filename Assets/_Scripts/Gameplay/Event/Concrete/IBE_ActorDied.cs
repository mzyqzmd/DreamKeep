namespace _DreamKeep.Gameplay
{
    /// <summary>单位死亡：由结算管线在血量归零时写出。</summary>
    public struct IBE_ActorDied : IBattleEvent
    {
        public Actor Actor { get; }

        public IBE_ActorDied(Actor actor_) => Actor = actor_;
    }
}
