using System;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>抽牌堆视图：提供抽牌起点锚点，抽牌入场演出从该点起飞；抽牌演出播完后把剩余张数写到计数文本。
    /// 抽牌堆见底时域侧会先在抽牌事件之前广播洗回事件，本视图据此静候弃牌堆那一摞飞过来，再把计数一张一张涨到洗回张数并落定弹一下——涨满后才轮到抽牌入场。</summary>
    public class UI_DrawPile : MonoBehaviour
    {
        [Tooltip("计数文本：显示抽牌堆剩余张数")] [SerializeField]
        private TextMeshProUGUI _countText;

        private CardPile _drawPile;
        private RectTransform _rect;

        public Vector3 Anchor => transform.position;

        #region 接线：抽牌演出播完后刷新剩余张数

        [Inject]
        public void Construct(BattleContext battleContext_, BattleEventDispatcher eventDispatcher_)
        {
            _drawPile = battleContext_.Player.CombatState.DrawPile;
            _rect = transform as RectTransform;
            _countText.text = _drawPile.Count.ToString(); // 进战即显示：此时抽牌堆已是洗好的整副牌
            eventDispatcher_.Register<BE_CardDrawn>(RefreshCountAsync);
        }

        #endregion

        #region 洗回演出：静候弃牌堆飞来，再把计数一张一张涨到洗回张数并弹一下

        public async UniTask PlayRefillAsync(int count_, float flyDuration_, float stepDuration_,
            float punchDuration_, float punchScale_)
        {
            // 洗回后张数取活值（域侧此刻已把整摞弃牌搬进来、尚未抽牌），洗回前张数由它减洗回张数现算：见底时即为 0
            int end = _drawPile.Count;
            int start = Mathf.Max(0, end - count_);
            int steps = Mathf.Max(1, count_);
            _countText.text = start.ToString();

            // 先等弃牌堆那一摞飞过来（数字保持洗回前的值），落地后才一张一张入堆
            await UniTask.Delay(TimeSpan.FromSeconds(flyDuration_), DelayType.DeltaTime);
            for (int i = 1; i <= steps; i++)
            {
                _countText.text = (start + i).ToString();
                await UniTask.Delay(TimeSpan.FromSeconds(stepDuration_), DelayType.DeltaTime);
            }

            // 落定弹一下：起播即返回，不为它延长玩家等待
            _rect.DOPunchScale(Vector3.one * punchScale_, punchDuration_);
        }

        #endregion

        // 抽走一张后重读张数；洗回张数由洗回演出专职搬运，此处读到的是本次抽牌后的剩余值
        private UniTask RefreshCountAsync(BE_CardDrawn drawn_)
        {
            _countText.text = _drawPile.Count.ToString();
            return UniTask.CompletedTask;
        }
    }
}