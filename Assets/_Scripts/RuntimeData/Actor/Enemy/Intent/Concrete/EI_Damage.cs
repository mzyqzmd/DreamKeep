namespace _DreamKeep.RuntimeData
{
    /// <summary>伤害意图：对玩家结算指定次数、每次指定数值的伤害。</summary>
    public sealed class EI_Damage : EnemyIntent
    {
        public int Amount { get; }
        public int Times { get; }

        public EI_Damage(int amount_, int times_)
        {
            Amount = amount_;
            Times = times_;
        }
        public override string GetIntentName() => "攻势";
        public override string GetDescription() 
            => Times == 1 ? $"这名敌人将要对你发动一次攻击" : $"这名敌人将要对你发动{Times}次攻击";
    }
}
