using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>骇人头盔：打出费用达到阈值的牌时获得格挡。</summary>
    public sealed class RE_TerrifyingHelmet : RelicEffect_T<SO_TerrifyingHelmet>
    {
        public RE_TerrifyingHelmet(SO_TerrifyingHelmet data_, BattleContext context_, ActionSystem actions_,
            BattleEventDispatcher dispatcher_, RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            Dispatcher.Register<BE_CardPlayed>(OnCardPlayed);
        }

        private async UniTask OnCardPlayed(BE_CardPlayed ev_)
        {
            if (ev_.Card.Cost.Value < Data.CostThreshold) return;
            await PerformAsync(new BA_Block(null, PlayerTarget, Data.Block));
        }
    }
}
