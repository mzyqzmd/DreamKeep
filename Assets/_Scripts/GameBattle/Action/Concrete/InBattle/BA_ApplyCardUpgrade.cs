using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>应用手牌选择结果：把选中的战斗副本逐张升级并发事件，不写回局内卡组。</summary>
    public sealed class BA_ApplyCardUpgrade : BattleAction
    {
        private readonly IReadOnlyList<CardModel> _cards;

        public BA_ApplyCardUpgrade(Creature source_, List<Creature> targets_, IReadOnlyList<CardModel> cards_)
            : base(source_, targets_) => _cards = cards_;

        public override List<IBattleEvent> Execute(BattleContext battleContext_)
        {
            List<IBattleEvent> events = new();
            foreach (CardModel card in _cards)
            {
                card.Upgrade();
                events.Add(new BE_CardUpgraded(card));
            }

            return events;
        }
    }
}
