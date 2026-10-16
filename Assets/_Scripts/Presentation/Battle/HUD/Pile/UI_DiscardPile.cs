using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>弃牌堆视图：计数文本、进堆弹跳与逐张步进都在基类；离场的牌飞向本堆并缩到 0 后回收，每张落地即记一次进堆。
    /// 回合末整手弃牌是一波同帧落地，由打牌流程在飞完之后逐张涨上来；抽牌堆见底时域侧把本堆整摞洗回抽牌堆并广播洗回事件，
    /// 不演飞行，本堆图标与位置全程不动，只有数字逐张下掉——那一路是往外出，不弹。</summary>
    public sealed class UI_DiscardPile : UI_Pile
    {
        private readonly BQ_PileCount _query = new(EPileKind.Discard);
        private BattleQueryDispatcher _queryDispatcher;

        protected override int LiveCount => _queryDispatcher.PileCount(_query);

        [Inject]
        public void Construct(BattleQueryDispatcher queryDispatcher_)
        {
            _queryDispatcher = queryDispatcher_;
            SyncToLive(); // 进战即显示：开局无弃牌
        }
    }
}
