using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>弃牌堆视图：锚点、计数文本与进堆弹跳都在基类；离场的牌飞向本堆并缩到 0 后回收，每张落地即记一次进堆。
    /// 抽牌堆见底时域侧会把弃牌堆整摞洗回抽牌堆并广播洗回事件，牌从本堆飞向抽牌堆的演出由打牌流程借卡池里的卡面播放，
    /// 本堆图标与位置全程不动，只有数字随每张牌起飞逐张下掉——那一路是往外出，不弹。</summary>
    public sealed class UI_DiscardPile : UI_Pile
    {
        private CardPile _discardPile;
        protected override int LiveCount => _discardPile.Count;

        [Inject]
        public void Construct(BattleContext battleContext_)
        {
            _discardPile = battleContext_.Player.CombatState.DiscardPile;
            SyncToLive(); // 进战即显示：开局无弃牌
        }
    }
}
