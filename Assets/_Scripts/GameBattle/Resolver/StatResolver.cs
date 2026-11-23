using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>战斗数值求值入口：把攻击类修正（StatHook）与防护类钳制（SlipperyHook）的固定次序收在一处，
    /// 预览与结算共用，杜绝同一公式在多个调用点各自手写导致漂移。
    /// 只做纯求值、不改任何状态——滑溜消费、写血、死亡清理、掉血后 Hook 仍留在各动作内。
    /// 口径：applyModifiers=false 只跳过攻击类修正，滑溜钳制照常生效（药水/投掷不吃力量但吃滑溜）。</summary>
    public static class StatResolver
    {
        /// <summary>伤害求值：先按需走攻击类修正，再做滑溜钳制。顺序固定，目标可空。</summary>
        public static int ResolveDamage(BattleContext context_, EStatChannel channel_, Creature source_, Creature target_,
            int baseAmount_, bool applyModifiers_)
        {
            int amount = applyModifiers_
                ? StatHook.Resolve(context_, channel_, source_, target_, baseAmount_)
                : baseAmount_;
            return SlipperyHook.Clamp(target_, amount);
        }

        /// <summary>格挡求值：只走格挡修正通道，按牌加成由调用方在基数里加好（CardBonusHook）。</summary>
        public static int ResolveBlock(BattleContext context_, Creature source_, Creature target_, int baseAmount_) =>
            StatHook.Resolve(context_, EStatChannel.Block, source_, target_, baseAmount_);

        /// <summary>失血求值：不经攻击修正，只做滑溜钳制（失去生命对一切来源一视同仁）。</summary>
        public static int ResolveLoss(Creature target_, int baseAmount_) => SlipperyHook.Clamp(target_, baseAmount_);
    }
}
