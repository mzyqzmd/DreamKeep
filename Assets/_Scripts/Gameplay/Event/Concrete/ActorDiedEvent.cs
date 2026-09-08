namespace _DreamKeep.Gameplay
{
    /// <summary>单位死亡：由结算管线在血量归零时写出。</summary>
    public class ActorDiedEvent : IDomainEvent
    {
        public Actor Actor { get; }

        public ActorDiedEvent(Actor actor_) => Actor = actor_;
    }
}
