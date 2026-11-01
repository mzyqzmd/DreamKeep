using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>准备背包：首回合初始抽牌之后额外抽牌，每场只触发一次。</summary>
    public sealed class RE_BagOfPreparation : RelicEffect_T<SO_BagOfPreparation>
    {
        private bool _granted;

        public RE_BagOfPreparation(SO_BagOfPreparation data_, BattleContext context_, ActionSystem actions_,
            BattleEventDispatcher dispatcher_, RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            Dispatcher.Register<BE_PlayerTurnActiveStarted>(OnPlayerTurnActiveStarted);
        }

        private async UniTask OnPlayerTurnActiveStarted(BE_PlayerTurnActiveStarted ev_)
        {
            if (_granted) return;
            _granted = true;
            Creature self = Context.Player.Creature;
            List<Creature> target = PlayerTarget;
            BattleAction[] draws = new BattleAction[Data.Cards];
            for (int i = 0; i < draws.Length; i++) draws[i] = new BA_DrawCard(self, target);
            await PerformAsync(draws);
        }
    }
}
