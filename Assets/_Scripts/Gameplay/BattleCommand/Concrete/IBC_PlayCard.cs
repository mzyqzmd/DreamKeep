using _DreamKeep.RuntimeData;

namespace _DreamKeep.Gameplay
{
    /// <summary>打出一张卡：携带被打出的卡与指定的目标槽位，能量与目标合法性由 Domain 校验。</summary>
    public class IBC_PlayCard : InBattleCommand
    {
        public CardModel PlayedCard { get; }
        public int AimSlot { get; }

        public IBC_PlayCard(CardModel card_, int aimSlot_)
        {
            PlayedCard = card_;
            AimSlot = aimSlot_;
        }
    }
}
