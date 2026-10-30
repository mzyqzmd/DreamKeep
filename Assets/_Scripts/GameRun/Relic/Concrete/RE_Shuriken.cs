using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>手里剑：同一回合内每打出 N 张攻击牌，获得指定状态的层数（默认力量）。
    /// 出牌事件在动作排空循环内播放，入队的挂状态动作由当前轮继续出队；回合开始时重置计数。</summary>
    public sealed class RE_Shuriken : RelicEffect
    {
        private readonly SO_Shuriken _data;
        private readonly BattlePerk _perk; // 每次触发 Clone 出独立实例交给目标，避免原型被持有后污染
        private int _attackCount;

        public RE_Shuriken(SO_Shuriken data_, BattleContext context_, ActionSystem actions_, BattleEventDispatcher dispatcher_,
            RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            _data = data_;
            _perk = PerkBaker.Bake(data_.Strength);
            if (_perk != null) _perk.AddLayers(data_.Layers);
            Dispatcher.Register<BE_CardPlayed>(OnCardPlayed);
            Dispatcher.Register<BE_TurnStarted>(OnTurnStarted);
        }

        private void OnTurnStarted(BE_TurnStarted ev_)
        {
            if (ev_.IsPlayerTurn) _attackCount = 0;
        }

        private async UniTask OnCardPlayed(BE_CardPlayed ev_)
        {
            if (ev_.Card.Type != ECardType.Attack) return;
            if (++_attackCount < _data.CardCount) return;
            _attackCount = 0;
            Creature self = Context.Player.Creature;
            await PerformAsync(new BA_ApplyPerk(self, new List<Creature> { self }, _perk.Clone()));
        }
    }
}
