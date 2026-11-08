using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>抽牌堆视图：计数文本、进堆弹跳与逐张步进都在基类；抽牌入场演出从本堆起飞，抽走一张即离堆一张。
    /// 数字不由域侧事件推动：抽牌时由打牌流程在牌起飞前记一次离堆；洗回不演飞行，整摞由打牌流程逐张涨上来——
    /// 涨满（每张 0.04 秒、整段最多 0.6 秒）后才轮到抽牌入场。</summary>
    public sealed class UI_DrawPile : UI_Pile
    {
        private BattleQueryDispatcher _queryDispatcher;

        protected override int LiveCount => _queryDispatcher.PileCount(EPileType.Draw);

        protected override EPileType Type => EPileType.Draw;

        [Inject]
        public void Construct(BattleQueryDispatcher queryDispatcher_)
        {
            _queryDispatcher = queryDispatcher_;
            SyncToLive(); // 进战即显示：此时抽牌堆已是洗好的整副牌
        }
    }
}
