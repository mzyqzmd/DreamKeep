using System;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>弃牌堆视图：提供归宿终点锚点，离场的牌飞向该点并缩到 0 后回收；弃牌演出播完后把已弃张数写到计数文本。
    /// 抽牌堆见底时域侧会把弃牌堆整摞洗回抽牌堆并广播洗回事件，届时本视图只负责把计数逐张掉到零——
    /// 牌从本堆飞向抽牌堆的演出由打牌流程借卡池里的卡面播放，本堆图标与位置全程不动。</summary>
    public class UI_DiscardPile : MonoBehaviour
    {
        [Tooltip("计数文本：显示弃牌堆已弃张数")] [SerializeField]
        private TextMeshProUGUI _countText;

        private CardPile _discardPile;

        public Vector3 Anchor => transform.position;

        #region 接线：弃牌演出播完后刷新已弃张数

        [Inject]
        public void Construct(BattleContext battleContext_, BattleEventDispatcher eventDispatcher_)
        {
            _discardPile = battleContext_.Player.CombatState.DiscardPile;
            _countText.text = _discardPile.Count.ToString(); // 进战即显示：开局无弃牌
            eventDispatcher_.Register<BE_CardDiscarded>(RefreshCountAsync);
        }

        #endregion

        #region 洗回演出：先静候牌飞走，再把计数逐张掉到零

        public async UniTask PlayDrainAsync(int count_, float delayDuration_, float stepDuration_)
        {
            // 洗回前张数：域侧此刻已清空本堆，用"活值 + 洗回张数"现算，将来部分洗回也正确
            int start = Mathf.Max(0, _discardPile.Count + count_);
            int steps = Mathf.Max(1, count_);

            await UniTask.Delay(TimeSpan.FromSeconds(delayDuration_), DelayType.DeltaTime);
            for (int i = 1; i <= steps; i++)
            {
                _countText.text = (start - i).ToString();
                await UniTask.Delay(TimeSpan.FromSeconds(stepDuration_), DelayType.DeltaTime);
            }
        }

        #endregion

        // 弃牌堆唯一会减少的路径是整体洗回抽牌堆（由洗回演出专职处理），此处只管弃牌
        private UniTask RefreshCountAsync(BE_CardDiscarded discarded_)
        {
            _countText.text = _discardPile.Count.ToString();
            return UniTask.CompletedTask;
        }
    }
}
