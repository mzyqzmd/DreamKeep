using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>闪亮口红：指定回合的玩家回合开始时获得力量与敏捷。</summary>
    public sealed class RE_Lipstick : RelicEffect<SO_Lipstick>
    {
        private readonly BattlePerk _strength;
        private readonly BattlePerk _dexterity;

        public RE_Lipstick(SO_Lipstick data_, BattleContext context_, ActionSystem actions_, BattleEventDispatcher dispatcher_,
            RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            _strength = PerkBaker.Bake(data_.Strength);
            if (_strength != null) _strength.AddLayers(data_.StrengthLayers);
            _dexterity = PerkBaker.Bake(data_.Dexterity);
            if (_dexterity != null) _dexterity.AddLayers(data_.DexterityLayers);
            Dispatcher.Register<BE_PlayerTurnActiveStarted>(OnPlayerTurnActiveStarted);
        }

        private async UniTask OnPlayerTurnActiveStarted(BE_PlayerTurnActiveStarted ev_)
        {
            if (Context.TurnCount != Data.Turn) return;
            Creature self = Context.Player.Creature;
            List<BattleAction> actions = new();
            if (_strength != null) actions.Add(new BA_ApplyPerk(self, PlayerTarget, _strength.Clone()));
            if (_dexterity != null) actions.Add(new BA_ApplyPerk(self, PlayerTarget, _dexterity.Clone()));
            if (actions.Count == 0) return;
            await PerformAsync(actions.ToArray());
        }
    }
}
