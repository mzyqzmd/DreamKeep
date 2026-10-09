namespace _DreamKeep.RuntimeData
{
    /// <summary>格挡意图：让自己获得指定数值的格挡。</summary>
    public sealed class EI_Block : EnemyIntent
    {
        public int Amount { get; }

        public EI_Block(int amount_) => Amount = amount_;
        public override string GetIntentName() => "防御";
        public override string GetDescription() => $"这名敌人将要获得格挡";
    }
}
