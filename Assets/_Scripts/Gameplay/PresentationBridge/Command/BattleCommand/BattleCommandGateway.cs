using _DreamKeep.RuntimeData;

namespace _DreamKeep.Gameplay
{
    /// <summary>命令发送门面实现：位于 Gameplay 内，将语义化意图解析为具体命令后入队战斗循环。</summary>
    public class BattleCommandGateway 
    {
        private readonly BattleLoop _battleLoop;

        public BattleCommandGateway(BattleLoop battleLoop_)
        {
            _battleLoop = battleLoop_;
        }

        public void PlayCard(CardModel card_, int aimSlot_)
            => _battleLoop.EnqueueCommand(new BC_PlayCard(card_, aimSlot_));

        public void EndTurn()
            => _battleLoop.EnqueueCommand(new BC_PlayerEndTurn());
    }
}
