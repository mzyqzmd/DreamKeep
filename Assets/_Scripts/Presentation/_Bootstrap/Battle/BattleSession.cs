using System;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.GameRun;

namespace _DreamKeep.Presentation
{
    /// <summary>段会话句柄：整局层与战斗段之间唯一的对象通道。
    /// 段在容器构建完成时写入自己的门面、销毁前清空；常驻 UI（顶栏 / 遗物栏 / 药水栏）只认这一个句柄，段不存在时一律静默不动作。
    /// 不让整局容器注册段内对象（VContainer 的子 scope 单向），也不让常驻层订阅战斗事件（分发器无注销，旧委托会在下一场被命中）。</summary>
    public sealed class BattleSession
    {
        public BattleCommandDispatcher Command { get; private set; }
        public BattleQueryDispatcher Query { get; private set; }
        public BattleEventDispatcher Events { get; private set; }
        public BattleTargetDirector TargetDirector { get; private set; }
        public UI_ActorRoot ActorRoot { get; private set; }
        public RelicTriggerNotifier Relics { get; private set; }
        public BattleLoop Loop { get; private set; }

        /// <summary>本场遭遇的奖励配置（可能为空：Boss 等终局遭遇不配奖励）；整局层据此在胜利后掷奖励。</summary>
        public SO_EncounterReward Reward { get; private set; }

        /// <summary>当前是否有段在场：常驻 UI 的每个对外入口都先问它。</summary>
        public bool IsOpen => Loop != null;

        /// <summary>是否处于战斗进行中：段在场且战斗循环未结束；领奖期（战斗已结束、段还在）为 false。</summary>
        public bool IsFighting => IsOpen && !Loop.IsFinished;

        /// <summary>段开关时触发：常驻层借此重绑段内引用与事件订阅。</summary>
        public event Action Changed;

        #region 开关会话：段容器构建完成时写入，段销毁前清空

        public void Open(BattleCommandDispatcher command_, BattleQueryDispatcher query_, BattleEventDispatcher events_,
            BattleTargetDirector targetDirector_, UI_ActorRoot actorRoot_, RelicTriggerNotifier relics_, BattleLoop loop_,
            SO_EncounterReward reward_)
        {
            Command = command_;
            Query = query_;
            Events = events_;
            TargetDirector = targetDirector_;
            ActorRoot = actorRoot_;
            Relics = relics_;
            Loop = loop_;
            Reward = reward_;
            Changed?.Invoke();
        }

        // 必须先关句柄再销毁段：常驻药水栏要在旧段的目标中间件还活着时注销瞄准
        public void Close()
        {
            Command = null;
            Query = null;
            Events = null;
            TargetDirector = null;
            ActorRoot = null;
            Relics = null;
            Loop = null;
            Reward = null;
            Changed?.Invoke();
        }

        #endregion
    }
}
