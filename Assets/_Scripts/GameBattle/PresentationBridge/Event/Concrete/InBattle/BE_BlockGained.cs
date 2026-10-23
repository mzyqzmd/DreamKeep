using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>格挡获得：记录目标与获得的格挡值。</summary>
    public struct BE_BlockGained : IBattleEvent
    {
        public Creature Target { get; }
        public int Amount { get; }

        public BE_BlockGained(Creature target_, int amount_)
        {
            Target = target_;
            Amount = amount_;
        }
    }
}
