using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;

namespace _DreamKeep.GameRun
{
    /// <summary>遗物效果泛型基座：把子类专属配置以 Data 暴露，消掉"声明字段 + 构造赋值"的逐类样板；
    /// 构造参数与转发链与非泛型基类完全一致。无专属配置读取需求的效果类直接继承非泛型 RelicEffect。</summary>
    public abstract class RelicEffect<TData> : RelicEffect where TData : SO_Relic
    {
        /// <summary>本件遗物的专属配置（子类具体类型，免除强转）。</summary>
        protected readonly TData Data;

        protected RelicEffect(TData data_, BattleContext context_, ActionSystem actions_,
            BattleEventDispatcher dispatcher_, RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_) => Data = data_;
    }
}
