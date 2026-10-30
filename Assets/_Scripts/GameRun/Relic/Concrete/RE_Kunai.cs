using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>苦无：每回合每打出 N 张攻击牌，获得敏捷；回合开始时重置计数。</summary>
    public sealed class RE_Kunai : RelicEffect
    {
        private readonly SO_Kunai _data;
        private readonly BattlePerk _perk;
        private int _count;

        public RE_Kunai(SO_Kunai data_, BattleContext context_, ActionSystem actions_, BattleEventDispatcher dispatcher_,
            RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            _data = data_;
            _perk = PerkBaker.Bake(data_.Dexterity);
            if (_perk != null) _perk.AddLayers(data_.Layers);
            Dispatcher.Register<BE_TurnStarted>(OnTurnStarted);
            Dispatcher.Register<BE_CardPlayed>(OnCardPlayed);
        }

        private void OnTurnStarted(BE_TurnStarted ev_)
        {
            if (ev_.IsPlayerTurn) _count = 0;
        }

        private async UniTask OnCardPlayed(BE_CardPlayed ev_)
        {
            if (_perk == null || ev_.Card.Type != ECardType.Attack) return;
            if (++_count < _data.Count) return;
            _count = 0;
            Creature self = Context.Player.Creature;
            await PerformAsync(new BA_ApplyPerk(self, new List<Creature> { self }, _perk.Clone()));
        }
    }
}
