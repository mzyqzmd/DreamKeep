using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>遗物效果泛型基座：把子类专属配置以 Data 暴露，消掉"声明字段 + 构造赋值"的逐类样板；
    /// 构造参数与转发链与非泛型基类完全一致。无专属配置读取需求的效果类直接继承非泛型 RelicEffect。</summary>
    public abstract class RelicEffect_T<TData> : RelicEffect where TData : SO_Relic
    {
        /// <summary>本件遗物的专属配置（子类具体类型，免除强转）。</summary>
        protected readonly TData Data;

        protected RelicEffect_T(TData data_, BattleContext context_, ActionSystem actions_,
            BattleEventDispatcher dispatcher_, RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_) => Data = data_;
    }

    /// <summary>遗物效果基座：每件遗物一个子类，构造时订阅关心的战斗事件，触发时构造既有 BattleAction 交动作系统执行。
    /// 效果只组合既有机制、不新增动作与数值管线；执行前先经播报器广播"本件遗物已生效"，表现层据此点亮对应槽位。
    /// 效果实例随战斗容器销毁，无需注销订阅。</summary>
    public abstract class RelicEffect
    {
        protected readonly BattleContext Context;
        protected readonly ActionSystem Actions;
        protected readonly BattleEventDispatcher Dispatcher;
        protected readonly RelicTriggerNotifier Notifier;
        protected readonly SO_Relic Relic;

        // 战斗内型节奏计数：住效果实例（随战斗新建/销毁），下场战斗归零；跨节点型不用此字段、直接读写 Player 计数表
        protected int Counter;

        protected RelicEffect(SO_Relic relic_, BattleContext context_, ActionSystem actions_,
            BattleEventDispatcher dispatcher_, RelicTriggerNotifier notifier_)
        {
            Relic = relic_;
            Context = context_;
            Actions = actions_;
            Dispatcher = dispatcher_;
            Notifier = notifier_;
        }

        #region 执行动作：入队后立即排空，动作演出被完整等待在本事件的分派链内；

        // 若排空已在进行中（出牌反应类事件正由队列循环播放），守卫直接放行、动作由当前轮继续出队

        protected async UniTask PerformAsync(params BattleAction[] actions_)
        {
            // 触发播报同步发出：涟漪与动作演出同帧开始，战斗节奏不受表现时长影响
            Notifier.NotifyTriggered(Relic);
            for (int i = 0; i < actions_.Length; i++)
                Actions.Enqueue(actions_[i]);
            await Actions.PerformAllAsync();
        }

        #endregion

        /// <summary>广播本件遗物的节奏计数值；跨节点型效果传入 Player 计数表读出的当前值。</summary>
        protected void NotifyCount(int count_) => Notifier.NotifyCount(Relic, count_);

        /// <summary>以玩家为目标的单体目标表：每次访问新建实例，与调用点内联 new 逐字等价。</summary>
        protected List<Creature> PlayerTarget => new() { Context.Player.Creature };
    }
}