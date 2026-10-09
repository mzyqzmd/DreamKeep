using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>消耗堆视图：锚点、计数文本与进堆弹跳都在基类；带消耗与遗忘标记的牌飞向本堆并缩到 0 后回收，每张落地即记一次进堆。
    /// 遗忘的牌只借道此处回收、域侧不入本堆（永久移出卡组），故不计数、也不弹。</summary>
    public sealed class UI_ExhaustPile : UI_Pile
    {
        private CardPile _exhaustPile;
        protected override int LiveCount => _exhaustPile.Count;

        [Inject]
        public void Construct(BattleContext battleContext_)
        {
            _exhaustPile = battleContext_.Player.CombatState.ExhaustPile;
            SyncToLive(); // 进战即显示：开局无消耗
        }
    }
}
