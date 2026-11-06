using System;
using _DreamKeep.GameBattle;

namespace _DreamKeep.GameRun
{
    /// <summary>段会话句柄：整局层与战斗段之间唯一的域对象通道（命令 / 查询 / 事件 / 循环）。
    /// 段在容器构建完成时写入自己的门面、销毁前清空；常驻 UI（顶栏 / 遗物栏 / 药水栏）只认这一个句柄，段不存在时一律静默不动作。
    /// 表现锚点（战场单位根 / 目标裁决中间件）已分家到表现侧句柄 BattleStage，两者成对开关。
    /// 不让整局容器注册段内对象（VContainer 的子 scope 单向），也不让常驻层订阅战斗事件（分发器无注销，旧委托会在下一场被命中）。</summary>
    public sealed class BattleSession
    {
        public BattleCommandDispatcher Command { get; private set; }
        public BattleQueryDispatcher Query { get; private set; }
        public BattleEventDispatcher Events { get; private set; }
        public BattleLoop Loop { get; private set; }

        /// <summary>当前是否有段在场：常驻 UI 的每个对外入口都先问它。</summary>
        public bool IsOpen => Loop != null;

        /// <summary>是否处于战斗进行中：段在场且战斗循环未结束；领奖期（战斗已结束、段还在）为 false。</summary>
        public bool IsFighting => IsOpen && !Loop.IsFinished;

        /// <summary>段开关时触发：常驻层借此重绑段内引用与事件订阅。</summary>
        public event Action Changed;

        #region 开关会话：段容器构建完成时写入，段销毁前清空

        public void Open(BattleCommandDispatcher command_, BattleQueryDispatcher query_,
            BattleEventDispatcher events_, BattleLoop loop_)
        {
            Command = command_;
            Query = query_;
            Events = events_;
            Loop = loop_;
            Changed?.Invoke();
        }

        // 必须先于 BattleStage.Close 调用：常驻药水栏借旧表现引用注销瞄准，此时旧中间件还活着
        public void Close()
        {
            Command = null;
            Query = null;
            Events = null;
            Loop = null;
            Changed?.Invoke();
        }

        #endregion
    }
}
