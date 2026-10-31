using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>遗忘之魂：每消耗一张牌，对随机敌人造成伤害（遗物伤害不吃力量与易伤）。</summary>
    public sealed class RE_ForgottenSoul : RelicEffect<SO_ForgottenSoul>
    {
        public RE_ForgottenSoul(SO_ForgottenSoul data_, BattleContext context_, ActionSystem actions_,
            BattleEventDispatcher dispatcher_, RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            Dispatcher.Register<BE_CardExhausted>(OnCardExhausted);
        }

        private async UniTask OnCardExhausted(BE_CardExhausted ev_) =>
            await PerformAsync(new BA_DamageRandomEnemy(null, Data.Damage, false));
    }
}
