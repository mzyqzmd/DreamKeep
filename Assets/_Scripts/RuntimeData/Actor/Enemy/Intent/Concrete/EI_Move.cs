namespace _DreamKeep.RuntimeData
{
    /// <summary>前进意图：向玩家方向前进一个槽位；前槽被占则白过，站最前槽位则回绕到最靠后空槽。</summary>
    public sealed class EI_Move : EnemyIntent
    {
        public override string GetIntentName() => "前进";
        public override string GetDescription() => "这名敌人将要向玩家方向前进一个槽位，站最前时触发特殊意图";
    }
}