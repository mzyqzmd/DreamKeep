using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using Cysharp.Threading.Tasks;

namespace _DreamKeep.GameBattle
{
    /// <summary>战斗结束终态：按被遗忘的局内卡幂等回写卡组，派发战斗结束事件，随后清空战斗态并静默停驻。</summary>
    public class BP_BattleFinish : BattlePhase
    {
        public BP_BattleFinish(StateMachine<BaseState<BattleLoop>> stateMachine_, BattleLoop owner_)
            : base(stateMachine_, owner_)
        {
        }

        public override string PhaseName => "战斗结束";
        public override bool IsWaiting => true;
        public override BaseState<BattleLoop> Next => null;

        #region 阶段异步逻辑：卡组回写 + 结束事件 + 清空战斗态

        public override async UniTask RunAsync()
        {
            BattleContext context = _owner.BattleContext;
            Player player = context.Player;

            // 遗忘是打出即永久移除，与胜负无关，故不论胜败都回写卡组，Remove 幂等
            foreach (CardModel runCard in context.ErasedRunCards)
                player.Deck.Remove(runCard);

            await PlayEvents(new BE_BattleEnded(context.IsVictory));

            // 表现层已收到结束事件，战斗态可以清空；局内卡组保留
            player.EndCombat();
            await base.RunAsync();
        }

        #endregion
    }
}
