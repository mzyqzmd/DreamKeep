using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using Cysharp.Threading.Tasks;

namespace _DreamKeep.GameBattle
{
    /// <summary>战斗结束阶段：派发战斗结束事件，随后清空战斗态。
    /// Next 恒为 null，是全流程唯一的终态——主循环据此停驻，不再推进也不重跑本阶段。</summary>
    public sealed class BP_BattleFinish : BattlePhase
    {
        public BP_BattleFinish(BattleLoop owner_) : base(owner_)
        {
        }

        public override BaseState<BattleLoop> Next => null;

        #region 阶段异步逻辑：结束事件 + 清空战斗态

        public override async UniTask RunAsync()
        {
            BattleContext context = _owner.BattleContext;
            Player player = context.Player;

            await PlayEvent(new BE_BattleEnded(context.IsVictory));

            // 表现层已收到结束事件，战斗态可以清空；局内卡组保留（遗忘已在结算当刻写回）
            player.EndCombat();
        }

        #endregion
    }
}