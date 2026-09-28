namespace _DreamKeep.RuntimeData
{
    /// <summary>治疗意图：恢复自己指定数值的生命。</summary>
    public class EI_Heal : EnemyIntent
    {
        public int Amount { get; }

        public EI_Heal(int amount_) => Amount = amount_;
    }
}
