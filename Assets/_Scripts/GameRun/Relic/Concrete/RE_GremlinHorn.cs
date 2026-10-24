using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>地精之角：每当有一名敌人死亡时，获得能量并抽牌。
    /// 玩家与敌人没有类型标记，按生物引用比较判定阵营。</summary>
    public sealed class RE_GremlinHorn : RelicEffect
    {
        private readonly SO_GremlinHorn _data;

        public RE_GremlinHorn(SO_GremlinHorn data_, BattleContext context_, ActionSystem actions_, BattleEventDispatcher dispatcher_,
            RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            _data = data_;
            Dispatcher.Register<BE_ActorDied>(OnActorDied);
        }

        private async UniTask OnActorDied(BE_ActorDied ev_)
        {
            if (ev_.Creature == Context.Player.Creature) return;
            List<Creature> self = new() { Context.Player.Creature };
            Creature source = Context.Player.Creature;
            BattleAction[] actions = new BattleAction[1 + _data.Cards];
            actions[0] = new BA_GainEnergy(source, self, _data.Energy);
            for (int i = 0; i < _data.Cards; i++)
                actions[i + 1] = new BA_DrawCard(source, self);
            await PerformAsync(actions);
        }
    }
}
