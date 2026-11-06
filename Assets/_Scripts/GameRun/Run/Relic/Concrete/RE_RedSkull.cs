using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>红头骨：生命比例跌破阈值时获得力量。
    /// 原作是"生命不高于阈值期间"的开关型状态，而现有状态没有单条移除通道，
    /// 故刻意做成每场首次跌破时一次性给层——这是本批唯一的语义近似。</summary>
    public sealed class RE_RedSkull : RelicEffect_T<SO_RedSkull>
    {
        private readonly BattlePerk _perk;
        private bool _granted;

        public RE_RedSkull(SO_RedSkull data_, BattleContext context_, ActionSystem actions_, BattleEventDispatcher dispatcher_,
            RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            _perk = PerkBaker.Bake(data_.Strength);
            if (_perk != null) _perk.AddLayers(data_.Layers);
            Dispatcher.Register<BE_BattleStarted>(OnBattleStarted);
            Dispatcher.Register<BE_DamageDealt>(ev_ => OnPlayerHurtAsync(ev_.Target));
            Dispatcher.Register<BE_HealthLost>(ev_ => OnPlayerHurtAsync(ev_.Target));
        }

        private async UniTask OnBattleStarted(BE_BattleStarted ev_) => await TryGrantAsync();

        // 伤害命中或生命流失任一到达即算跌破信号：两条路径同一守卫
        private async UniTask OnPlayerHurtAsync(Creature target_)
        {
            if (target_ != Context.Player.Creature) return;
            await TryGrantAsync();
        }

        private async UniTask TryGrantAsync()
        {
            if (_granted || _perk == null) return;
            Creature self = Context.Player.Creature;
            if (self.CurrentHp > self.MaxHp * Data.HpThreshold) return;
            _granted = true;
            await PerformAsync(new BA_ApplyPerk(self, PlayerTarget, _perk.Clone()));
        }
    }
}
