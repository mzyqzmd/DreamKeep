using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>算盘：每次弃牌堆洗回抽牌堆时获得格挡。</summary>
    public sealed class RE_Abacus : RelicEffect
    {
        private readonly SO_Abacus _data;

        public RE_Abacus(SO_Abacus data_, BattleContext context_, ActionSystem actions_, BattleEventDispatcher dispatcher_,
            RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            _data = data_;
            Dispatcher.Register<BE_DiscardReshuffled>(OnDiscardReshuffled);
        }

        private async UniTask OnDiscardReshuffled(BE_DiscardReshuffled ev_) =>
            await PerformAsync(new BA_Block(null, new List<Creature> { Context.Player.Creature }, _data.Block));
    }
}
