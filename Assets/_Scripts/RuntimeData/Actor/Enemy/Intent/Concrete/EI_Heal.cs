namespace _DreamKeep.RuntimeData
{
    /// <summary>治疗意图：恢复自己指定数值的生命。</summary>
    public class EI_Heal : EnemyIntent
    {
        public int Amount { get; }

        public EI_Heal(int amount_) => Amount = amount_;

        public override string GetDescription() => $"接下来恢复自身 {Amount} 点生命";
    }
}
