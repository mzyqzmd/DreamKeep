namespace _DreamKeep.RuntimeData
{
    /// <summary>击晕意图：本回合不行动，执行完恢复原本意图且不推进意图序列。
    /// 本类无字段，所有被击晕的敌人可安全共享同一实例。</summary>
    public sealed class EI_Stun : EnemyIntent
    {
        public override string GetIntentName() => "击晕";
        public override string GetDescription() => "这名敌人被击晕，本回合无法行动，\n结束后恢复原本意图";
    }
}
