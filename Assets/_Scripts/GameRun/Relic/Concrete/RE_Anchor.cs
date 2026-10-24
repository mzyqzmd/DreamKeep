using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>锚：每场战斗开始时获得格挡。
    /// 挂首回合「回合开始」事件——清格挡在其之前已执行完，此处获得的格挡不会被清；
    /// 挂战斗开始事件反而会先加挡再被清（回合开始无条件 BA_ClearBlock）。</summary>
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
            await PerformAsync(new BA_Block(self, new List<Creature> { self }, _data.Block));
        }
    }
}
