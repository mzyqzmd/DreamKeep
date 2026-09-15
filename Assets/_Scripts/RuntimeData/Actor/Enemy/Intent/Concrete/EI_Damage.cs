namespace _DreamKeep.RuntimeData
{
    /// <summary>伤害意图：对玩家结算指定次数、每次指定数值的伤害。</summary>
    public class EI_Damage : EnemyIntent
    {
        public int Amount { get; }
        public int Times { get; }

        public EI_Damage(int amount_, int times_)
        {
            Amount = amount_;
            Times = times_;
        }
    }
}
