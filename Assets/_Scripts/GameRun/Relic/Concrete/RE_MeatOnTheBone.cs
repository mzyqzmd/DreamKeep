using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameRun
{
    /// <summary>带骨肉：战斗结束时生命比例不高于阈值则回复生命。
    /// 结束事件播完才清空战斗态，处理器内排空来得及。</summary>
    public sealed class RE_MeatOnTheBone : RelicEffect
    {
        private readonly SO_MeatOnTheBone _data;

        public RE_MeatOnTheBone(SO_MeatOnTheBone data_, BattleContext context_, ActionSystem actions_,
            BattleEventDispatcher dispatcher_, RelicTriggerNotifier notifier_)
            : base(data_, context_, actions_, dispatcher_, notifier_)
        {
            _data = data_;
            Dispatcher.Register<BE_BattleEnded>(OnBattleEnded);
        }

        private async UniTask OnBattleEnded(BE_BattleEnded ev_)
        {
            if (!ev_.IsVictory) return; // 战败不触发
            Creature self = Context.Player.Creature;
            if (self.CurrentHp > self.MaxHp * _data.HpThreshold) return;
            await PerformAsync(new BA_Heal(null, new List<Creature> { self }, _data.Heal));
        }
    }
}
