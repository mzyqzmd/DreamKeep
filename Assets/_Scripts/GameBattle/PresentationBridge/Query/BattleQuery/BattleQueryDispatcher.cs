using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.GameBattle
{
    /// <summary>上行查询门面：表现层唯一的只读提问出口，只读直算、不入队、无副作用。
    /// 内部转调玩法层既有解析与修正管线，与结算共用同一份规则（预览的是谁就一定打到谁）；视图不直接触碰战斗循环、战斗态与修正管线。
    /// 两类形态：边界型查询（牌堆张数）由独立 BQ_ 类型表达，每帧预览走方法调用（避免逐帧分配）。</summary>
    public class BattleQueryDispatcher
    {
        private readonly BattleContext _battleContext;

        public BattleQueryDispatcher(BattleContext battleContext_) => _battleContext = battleContext_;

        #region 查询：只读直算，不入队，无副作用

        /// <summary>查询指定牌堆当前的张数：走战斗态的牌堆分类映射，问句由调用方缓存复用。</summary>
        public int PileCount(BQ_PileCount query_)
            => _battleContext.Player.CombatState.Pile(query_.Kind).Count;

        /// <summary>存活敌人列表：按槽位排序，顺序即瞄准下标（aimSlot）的语义。</summary>
        public IReadOnlyList<Creature> GetAliveEnemies()
        {
            List<Enemy> alive = _battleContext.GetAliveEnemies();
            List<Creature> creatures = new(alive.Count);
            for (int i = 0; i < alive.Count; i++)
                creatures.Add(alive[i].Creature);
            return creatures;
        }

        /// <summary>预览这张牌在指定瞄准下标下会命中的目标：与结算共用同一份落点解析。</summary>
        public IReadOnlyList<Creature> PreviewTargets(CardModel card_, int aimSlot_)
            => CardAffixResolver.PreviewTargets(_battleContext, card_, _battleContext.Player, aimSlot_);

        /// <summary>预览玩家打出的卡造成的伤害：来源内定玩家，未瞄准目标时传 null（只算来源侧修正）。</summary>
        // TODO 修正来源显示：卡面/气泡将来要写"9（力量+2）"时，由表现层传入 sourceOut_ 接住贡献者列表（现在无人传，零开销）
        public int PreviewCardDamage(Creature target_, int baseAmount_, List<BattlePerk> sourceOut_ = null)
            => DamageHook.Resolve(_battleContext, _battleContext.Player.Creature, target_, baseAmount_, sourceOut_);

        /// <summary>预览敌人意图造成的伤害：目标内定玩家。</summary>
        // TODO 修正来源显示：意图图标将来要标出"谁改了这个值"时，由表现层传入 sourceOut_（现在无人传，零开销）
        public int PreviewIntentDamage(Creature source_, int baseAmount_, List<BattlePerk> sourceOut_ = null)
            => DamageHook.Resolve(_battleContext, source_, _battleContext.Player.Creature, baseAmount_, sourceOut_);

        #endregion
    }
}
