using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>命令分发器：表现层唯一的上行入口，把语义化意图转成具体命令后交给战斗循环排队，视图不直接触碰循环。</summary>
    public class BattleCommandDispatcher
    {
        private readonly BattleLoop _battleLoop;

        public BattleCommandDispatcher(BattleLoop battleLoop_)
        {
            _battleLoop = battleLoop_;
        }

        public void PlayCard(CardModel card_, int aimSlot_)
            => _battleLoop.EnqueueCommand(new BC_PlayCard(card_, aimSlot_));

        public void EndTurn()
            => _battleLoop.EnqueueCommand(new BC_PlayerEndTurn());
    }
}
