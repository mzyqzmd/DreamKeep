namespace _DreamKeep.RuntimeData
{
    /// <summary>格挡意图：让自己获得指定数值的格挡。</summary>
    public class EI_Block : EnemyIntent
    {
        public int Amount { get; }

        public EI_Block(int amount_) => Amount = amount_;

        public override string GetDescription() => $"接下来为自己获得 {Amount} 点格挡";
    }
}
