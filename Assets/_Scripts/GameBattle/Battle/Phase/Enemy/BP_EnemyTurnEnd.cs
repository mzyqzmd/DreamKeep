using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using Cysharp.Threading.Tasks;

namespace _DreamKeep.GameBattle
{
    /// <summary>敌人回合结束阶段：逐敌人结算回合末状态（中毒/再生）并衰减，随后推进回合计数，一回合到此结束。</summary>
    public sealed class BP_EnemyTurnEnd : BattlePhase
    {
        public BP_EnemyTurnEnd(BattleLoop owner_) : base(owner_)
        {
        }

        public override BaseState<BattleLoop> Next => _owner.PhasePlayerTurnStart;

        #region 阶段异步逻辑：逐敌人结算回合末状态并衰减，等待演出后推进回合计数

        public override async UniTask RunAsync()
        {
            BattleContext context = _owner.BattleContext;
            ActionSystem actions = _owner.ActionSystem;

            foreach (Enemy enemy in context.Enemies)
            {
                if (enemy.Creature.IsDead) continue;
                List<Creature> self = new List<Creature> { enemy.Creature };

                // 回合末状态结算（中毒 → 再生）先于衰减：宿主自己的回合边界
                List<BattleAction> triggered = new();
                HookTrigger.CollectTurnEnd(enemy.Creature, self, triggered);
                for (int i = 0; i < triggered.Count; i++)
                    actions.Enqueue(triggered[i]);

                actions.Enqueue(new BA_DecayPerk(enemy.Creature, self, EPerkDecayTiming.TurnEnd));
            }

            await actions.PerformAllAsync();

            // 唯一例外：回合计数无来源无目标且不发事件，不做成动作，直接推进
            context.TurnCount++;
        }

        #endregion
    }
}