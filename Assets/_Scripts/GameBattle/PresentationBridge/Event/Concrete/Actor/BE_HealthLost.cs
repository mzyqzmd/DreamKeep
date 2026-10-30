using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>生命流失：记录失去生命的目标与数值（无视格挡，不含格挡吸收）。</summary>
    public struct BE_HealthLost : IBattleEvent
    {
        public Creature Target { get; }
        public int Amount { get; }

        public BE_HealthLost(Creature target_, int amount_)
        {
            Target = target_;
            Amount = amount_;
        }
    }
}
