using System.Collections.Generic;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using Cysharp.Threading.Tasks;

namespace _DreamKeep.GameBattle
{
    /// <summary>战斗开始阶段：派发战斗开始事件，随后广播每名敌人的首条意图，再自动推进到首个玩家回合。</summary>
    public sealed class BP_BattleStart : BattlePhase
    {
        public BP_BattleStart(BattleLoop owner_) : base(owner_)
        {
        }

        public override BaseState<BattleLoop> Next => _owner.PhasePlayerTurnStart;

        #region 阶段异步逻辑：开始事件 + 首轮意图预告

        public override async UniTask RunAsync()
        {
            await PlayEvent(new BE_BattleStarted());

            List<Enemy> enemies = _owner.BattleContext.GetAliveEnemies();
            foreach (Enemy enemy in enemies)
                await PlayEvent(new BE_EnemyIntentChanged(enemy, enemy.PreviewIntent));
        }

        #endregion
    }
}
