using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using Cysharp.Threading.Tasks;

namespace _DreamKeep.GameBattle
{
    /// <summary>敌人回合开始阶段：清空敌人上回合残留的格挡，再结算回合开始时机（TurnStart）的状态，为意图结算做准备。</summary>
    public sealed class BP_EnemyTurnStart : BattlePhase
    {
        public BP_EnemyTurnStart(BattleLoop owner_) : base(owner_)
        {
        }

        public override BaseState<BattleLoop> Next => _owner.PhaseEnemyTurnActive;

        #region 阶段异步逻辑：逐敌人清格挡与结算状态后等待演出

        public override async UniTask RunAsync()
        {
            await PlayEvent(new BE_TurnStarted(_owner.BattleContext.TurnCount, false));

            // 回合开始即宣告本回合意图：台词由表现层按纯机制事实取词，宣告先于动作结算；
            // 按槽位从左到右，与执行阶段同序；被击晕整回合不行动的敌人不开口
            foreach (Enemy enemy in _owner.BattleContext.GetAliveEnemies())
            {
                if (enemy.IsStunned) continue;
                EnemyIntent intent = enemy.PreviewIntent;
                if (intent == null) continue;
                await PlayEvent(new BE_EnemyIntentAnnounced(enemy, enemy.IntentIndex,
                    intent != enemy.CurrentIntent, enemy.IsPhaseTwo));
            }

            ActionSystem actions = _owner.ActionSystem;

            foreach (Enemy enemy in _owner.BattleContext.Enemies)
            {
                if (enemy.Creature.IsDead) continue;
                List<Creature> self = new List<Creature> { enemy.Creature };
                actions.Enqueue(new BA_ClearBlock(enemy.Creature, self));
                actions.Enqueue(new BA_SettlePerk(enemy.Creature, self, EPerkSettleTiming.TurnStart));
            }

            await actions.PerformAllAsync();
        }

        #endregion
    }
}
