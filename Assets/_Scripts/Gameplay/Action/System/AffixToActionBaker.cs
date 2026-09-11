using System.Collections.Generic;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;

namespace _DreamKeep.Gameplay
{
    /// <summary>词条转动作工厂：把玩家打出卡的效果型词条按管线顺序转成动作入队，一个词条产出一个动作。</summary>
    public class AffixToActionBaker
    {
        private readonly ActionSystem _actionSystem;
        private readonly TargetResolver _targetResolver;

        public AffixToActionBaker(ActionSystem actionSystem_, TargetResolver targetResolver_)
        {
            _actionSystem = actionSystem_;
            _targetResolver = targetResolver_;
        }

        #region 建队：先解析落点，再按词条类型逐个转动作入队
        public void Build(BattleState battleState_, CardModel card_, Player player_, int aimSlot_)
        {
            List<Actor> opponents = _targetResolver.Resolve(battleState_, card_, aimSlot_);
            List<Actor> self = new List<Actor> { player_ };

            foreach (CardAffix affix in card_.ActiveAffixes)
                EnqueueByAffix(affix, player_, self, opponents, card_);
        }
        #endregion

        // 按词条类型转动作入队：伤害与状态作用于敌方，其余作用于自身
        private void EnqueueByAffix(CardAffix affix_, Player player_, List<Actor> self_, List<Actor> opponents_, CardModel card_)
        {
            switch (affix_)
            {
                case CA_Damage damage:
                    for (int i = 0; i < damage.Times.Value; i++)
                        _actionSystem.Enqueue(new BA_Damage(player_, opponents_, damage.Amount.Value));
                    break;
                case CA_Block block:
                    _actionSystem.Enqueue(new BA_Block(player_, self_, block.Amount.Value));
                    break;
                case CA_ApplyPerk applyPerk:
                    _actionSystem.Enqueue(new BA_ApplyPerk(player_, opponents_, applyPerk.Perk));
                    break;
                case CA_DrawCard drawCard:
                    for (int i = 0; i < drawCard.Count.Value; i++)
                        _actionSystem.Enqueue(new BA_DrawCard(player_, self_));
                    break;
                case CA_GainEnergy gainEnergy:
                    _actionSystem.Enqueue(new BA_GainEnergy(player_, gainEnergy.Amount.Value));
                    break;
                case CA_Grant grant:
                    // 每张被授予的卡各自持有一份克隆，避免多张卡共享同一词条实例
                    _actionSystem.Enqueue(new BA_Grant(player_, self_, grant.Count.Value, grant.GrantedAffix));
                    break;
                case CA_Erase _:
                    _actionSystem.Enqueue(new BA_Erase(player_, self_, card_));
                    break;
                case CA_Exhaust _:
                    _actionSystem.Enqueue(new BA_Exhaust(player_, self_, card_));
                    break;
            }
        }
    }
}
