using System.Collections.Generic;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using Cysharp.Threading.Tasks;

namespace _DreamKeep.GameBattle
{
    /// <summary>战斗开始阶段：派发战斗开始事件，随后广播每名敌人的首条意图，再自动推进到首个玩家回合。</summary>
    public class BP_BattleStart : BattlePhase
    {
        public BP_BattleStart(StateMachine<BaseState<BattleLoop>> stateMachine_, BattleLoop owner_)
            : base(stateMachine_, owner_)
        {
        }
        public override BaseState<BattleLoop> Next => _owner.PhasePlayerTurnStart;

        #region 阶段异步逻辑：开始事件 + 首轮意图预告

        public override async UniTask RunAsync()
        {
            await PlayEvents(new BE_BattleStarted());

            List<Enemy> enemies = _owner.BattleContext.GetAliveEnemies();
            foreach (Enemy enemy in enemies)
                await PlayEvents(new BE_EnemyIntentChanged(enemy, enemy.CurrentIntent));

            await base.RunAsync();
        }

        #endregion
    }
}
