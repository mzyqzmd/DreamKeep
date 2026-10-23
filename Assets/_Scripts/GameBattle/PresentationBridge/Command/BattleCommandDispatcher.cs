using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>上行命令门面：表现层唯一向下说话的出口，把语义化意图转成具体命令后交给战斗循环排队，视图不直接触碰循环。
    /// 只读提问（预览、敌方列表、牌堆张数）一律走 BattleQueryDispatcher（Query/BattleQuery），本类不承载查询。</summary>
    public class BattleCommandDispatcher
    {
        private readonly BattleLoop _battleLoop;

        public BattleCommandDispatcher(BattleLoop battleLoop_) => _battleLoop = battleLoop_;

        #region 命令：转成具体命令后交给战斗循环排队

        public void PlayCard(CardModel card_, int aimSlot_)
            => _battleLoop.EnqueueCommand(new BC_PlayCard(card_, aimSlot_));

        public void EndTurn()
            => _battleLoop.EnqueueCommand(new BC_PlayerEndTurn());

        #endregion
    }
}
