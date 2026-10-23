using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>消耗堆视图：计数文本、进堆弹跳与逐张步进都在基类；带消耗与遗忘标记的牌飞向本堆并缩到 0 后回收，每张落地即记一次进堆。
    /// 遗忘的牌只借道此处回收、域侧不入本堆（永久移出卡组），故不计数、也不弹。</summary>
    public sealed class UI_ExhaustPile : UI_Pile
    {
        private BattleQueryDispatcher _queryDispatcher;

        protected override int LiveCount => _queryDispatcher.PileCount(EPileType.Exhaust);

        protected override EPileType Type => EPileType.Exhaust;

        [Inject]
        public void Construct(BattleQueryDispatcher queryDispatcher_)
        {
            _queryDispatcher = queryDispatcher_;
            SyncToLive(); // 进战即显示：开局无消耗
        }
    }
}