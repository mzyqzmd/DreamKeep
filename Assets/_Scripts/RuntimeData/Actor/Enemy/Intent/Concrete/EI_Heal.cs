namespace _DreamKeep.RuntimeData
{
    /// <summary>治疗意图：恢复自己指定数值的生命。</summary>
    public sealed class EI_Heal : EnemyIntent
    {
        public int Amount { get; }

        public EI_Heal(int amount_) => Amount = amount_;
        public override string GetIntentName() => "治疗";
        public override string GetDescription() => $"这名敌人将要治疗自己";
    }
}
