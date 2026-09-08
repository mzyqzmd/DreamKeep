using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Gameplay
{
    /// <summary>词条转动作工厂：把一张卡的效果型词条按管线顺序转成动作入队，一个词条产出一个动作。</summary>
    public class AffixActionFactory
    {
        private readonly ActionSystem _actionSystem;
        private readonly TargetResolver _targetResolver;

        public AffixActionFactory(ActionSystem actionSystem_, TargetResolver targetResolver_)
        {
            _actionSystem = actionSystem_;
            _targetResolver = targetResolver_;
        }

        #region 建队：先解析落点，再按词条类型逐个转动作入队
        public void Build(BattleState battleState_, InBattleCard card_, Actor source_, int aimSlot_)
        {
            List<Actor> opponents = _targetResolver.Resolve(battleState_, card_, aimSlot_);
            List<Actor> self = new List<Actor> { source_ };

            foreach (CardAffix affix in card_.Affixes)
                EnqueueByAffix(affix, source_, self, opponents, card_);
        }
        #endregion

        #region 从任意词条集合建队：敌人意图与玩家卡共用同一条结算管线
        public void BuildFrom(IEnumerable<CardAffix> affixes_, Actor source_, List<Actor> self_, List<Actor> opponents_)
        {
            foreach (CardAffix affix in affixes_)
                EnqueueByAffix(affix, source_, self_, opponents_, null);
        }
        #endregion

        #region 按词条类型转动作入队：伤害与状态作用于敌方，其余作用于自身
        public void EnqueueByAffix(CardAffix affix_, Actor source_, List<Actor> self_, List<Actor> opponents_, InBattleCard card_)
        {
            switch (affix_)
            {
                case CA_Damage damage:
                    for (int i = 0; i < damage.Times.Value; i++)
                        _actionSystem.Enqueue(new Action_Damage(source_, opponents_, damage.Amount.Value));
                    break;
                case CA_Block block:
                    _actionSystem.Enqueue(new Action_Block(source_, self_, block.Amount.Value));
                    break;
                case CA_ApplyPerk applyPerk:
                    _actionSystem.Enqueue(new Action_ApplyPerk(source_, opponents_, applyPerk.Perk));
                    break;
                case CA_DrawCard drawCard:
                    for (int i = 0; i < drawCard.Count.Value; i++)
                        _actionSystem.Enqueue(new Action_DrawCard(source_, self_));
                    break;
                case CA_GainEnergy gainEnergy:
                    _actionSystem.Enqueue(new Action_GainEnergy(source_, self_, gainEnergy.Amount.Value));
                    break;
                case CA_Grant grant:
                    // 每张被授予的卡各自持有一份克隆，避免多张卡共享同一词条实例
                    _actionSystem.Enqueue(new Action_Grant(source_, self_, grant.Count.Value, grant.GrantedAffix));
                    break;
                case CA_Erase _:
                    if (card_ != null)
                        _actionSystem.Enqueue(new Action_Erase(source_, self_, card_.InstanceId));
                    break;
                case CA_Exhaust _:
                    if (card_ != null)
                        _actionSystem.Enqueue(new Action_Exhaust(source_, self_, card_.InstanceId));
                    break;
            }
        }
        #endregion
    }
}
