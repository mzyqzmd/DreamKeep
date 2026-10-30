using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>锚：每场战斗开始时获得格挡。
    /// 挂玩家回合开窗事件（清格挡与初始抽牌之后）——此刻加的格挡不会被本回合开始清掉；
    /// 挂 BE_TurnStarted 或战斗开始事件都会先加挡、再被回合开始的 BA_ClearBlock 清空。
    /// 格挡来源传 null：遗物格挡不吃敏捷。</summary>
    public sealed class RE_Anchor : RelicEffect
    {
        private readonly SO_Anchor _data;
        private bool _granted; // 每场一次：效果实例随战斗容器新建，天然逐场重置

        public RE_Anchor(SO_Anchor data_, BattleContext context_, ActionSystem actions_, BattleEventDispatcher dispatcher_,
            RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            _data = data_;
            Dispatcher.Register<BE_PlayerTurnActiveStarted>(OnPlayerTurnActiveStarted);
        }

        private async UniTask OnPlayerTurnActiveStarted(BE_PlayerTurnActiveStarted ev_)
        {
            if (_granted) return;
            _granted = true;
            Creature self = Context.Player.Creature;
            await PerformAsync(new BA_Block(null, new List<Creature> { self }, _data.Block));
        }
    }
}
