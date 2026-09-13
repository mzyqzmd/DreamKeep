using _DreamKeep.RuntimeData;

namespace _DreamKeep.Gameplay
{
    /// <summary>伤害结算完成：记录来源、目标、最终伤害与被格挡吸收的部分。</summary>
    public struct BE_DamageDealt : IBattleEvent
    {
        public Creature Source { get; }
        public Creature Target { get; }
        public int Amount { get; }
        public int Absorbed { get; }

        public BE_DamageDealt(Creature source_, Creature target_, int amount_, int absorbed_)
        {
            Source = source_;
            Target = target_;
            Amount = amount_;
            Absorbed = absorbed_;
        }
    }
}
