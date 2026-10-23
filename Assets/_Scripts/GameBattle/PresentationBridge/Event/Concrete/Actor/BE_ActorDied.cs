using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>单位死亡：由结算管线在血量归零时写出。</summary>
    public struct BE_ActorDied : IBattleEvent
    {
        public Creature Creature { get; }
        public Creature Killer { get; }

        public BE_ActorDied(Creature creature_, Creature killer_)
        {
            Creature = creature_;
            Killer = killer_;
        }
    }
}