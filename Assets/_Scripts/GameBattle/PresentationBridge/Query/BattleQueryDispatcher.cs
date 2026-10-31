using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>上行查询门面：表现层唯一的只读提问出口，只读直算、不入队、无副作用。
    /// 内部转调玩法层既有解析与 StatHook，与结算共用同一份规则（预览的是谁就一定打到谁）；视图不直接触碰战斗循环、战斗态与 StatHook。
    /// 两类形态：边界型查询（牌堆张数）由独立 BQ_ 类型表达，每帧预览走方法调用（避免逐帧分配）。</summary>
    public class BattleQueryDispatcher
    {
        private readonly BattleContext _battleContext;

        public BattleQueryDispatcher(BattleContext battleContext_) => _battleContext = battleContext_;

        #region 查询：只读直算，不入队，无副作用

        /// <summary>查询指定牌堆当前的张数：走战斗态的牌堆分类映射，问句由调用方缓存复用；战斗已结束（战斗态已清空）时为 0。</summary>
        public int PileCount(EPileType type_)
            => _battleContext.Player.CombatState?.Pile(type_).Count ?? 0;

        /// <summary>查询指定牌堆的全部卡（列表由调用方缓存复用，先清后填）：按堆内连续槽位键序 0..Count-1 填充，
        /// 索引即卡在堆中的顺序——弃牌/消耗堆即进入顺序，抽牌堆 index 0 为堆顶、抽一张少一张；战斗已结束时只清空结果。</summary>
        public void PileCards(EPileType type_, List<CardModel> results_)
        {
            results_.Clear();
            // 战斗结束会清空战斗态，而视图仍可能被点开：这里收敛成空表，不让表现层认识"战斗已结束"这个域事实
            PlayerCombatState state = _battleContext.Player.CombatState;
            if (state == null) return;
            IReadOnlyList<CardModel> cards = state.Pile(type_).Cards;
            for (int i = 0; i < cards.Count; i++) results_.Add(cards[i]);
        }

        /// <summary>存活敌人列表：按槽位排序，顺序即瞄准下标（aimSlot）的语义。</summary>
        public IReadOnlyList<Creature> GetAliveEnemies() => _battleContext.GetAliveCreatures();

        /// <summary>预览这张牌在指定瞄准下标下会命中的目标：与结算共用同一份落点解析。</summary>
        public IReadOnlyList<Creature> PreviewTargets(CardModel card_, int aimSlot_)
            => CardAffixResolver.PreviewTargets(_battleContext, card_, _battleContext.Player, aimSlot_);

        /// <summary>预览玩家打出的卡造成的伤害：来源内定玩家，未瞄准目标时传 null（只算来源侧修正）。</summary>
        public int PreviewCardDamage(Creature target_, int baseAmount_)
            => StatHook.Resolve(_battleContext, EStatChannel.Damage, _battleContext.Player.Creature, target_, baseAmount_);

        /// <summary>预览玩家打出的卡获得的格挡：来源内定玩家，格挡只吃来源侧修正（目标可传 null）。</summary>
        public int PreviewCardBlock(Creature target_, int baseAmount_)
            => StatHook.Resolve(_battleContext, EStatChannel.Block, _battleContext.Player.Creature, target_, baseAmount_);

        /// <summary>预览敌人意图造成的伤害：目标内定玩家。</summary>
        public int PreviewIntentDamage(Creature source_, int baseAmount_)
            => StatHook.Resolve(_battleContext, EStatChannel.Damage, source_, _battleContext.Player.Creature, baseAmount_);

        #endregion
    }
}