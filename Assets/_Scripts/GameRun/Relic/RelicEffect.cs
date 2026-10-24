using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;

namespace _DreamKeep.GameRun
{
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
    }
}