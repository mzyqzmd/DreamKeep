using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>抽牌堆视图：锚点、计数文本与进堆弹跳都在基类；抽牌入场演出从本堆起飞，抽走一张即离堆一张。
    /// 数字不由域侧事件推动：抽牌时由打牌流程在牌起飞前记一次离堆，洗回时由每张飞来的牌落地记一次进堆——
    /// 洗回的逐张涨数与逐张弹跳因此与飞行的节奏同源，涨满后才轮到抽牌入场。</summary>
    public class UI_DrawPile : UI_Pile
    {
        private CardPile _drawPile;
        protected override int LiveCount => _drawPile.Count;

        [Inject]
        public void Construct(BattleContext battleContext_)
        {
            _drawPile = battleContext_.Player.CombatState.DrawPile;
            SyncToLive(); // 进战即显示：此时抽牌堆已是洗好的整副牌
        }
    }
}
