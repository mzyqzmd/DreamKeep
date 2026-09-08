using _DreamKeep.RuntimeData;

namespace _DreamKeep.Gameplay
{
    /// <summary>打出一张卡：携带卡实例 Id 与指定的目标槽位，能量与目标合法性由 Domain 校验。</summary>
    public class BC_PlayCard : BattleCommand
    {
        public InBattleCard PlayedCard { get; private set; }
        public BC_PlayCard(InBattleCard card_) => PlayedCard = card_;
    }
}
