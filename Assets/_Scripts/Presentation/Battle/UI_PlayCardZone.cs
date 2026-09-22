using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>出牌区：打出的牌在此按先后排队堆叠——越靠后的牌越小，层内也越靠后（队首恒在最上），结算期间停在队位不动。
    /// 它是与手牌容器平级的隐形矩形：不挂图形、不参与指针命中，只回答"卡牌落点在不在本区矩形内"这一个问题，
    /// 并用"本区矩形中心 + 每张错开量"算出每个队位；矩形即唯一真源——往哪儿松手算打出、打出的牌停在哪儿都由这一块矩形定死。
    /// 卡牌坐标由装配点下发的框架（手牌中心节点）往返换算进来：本类不认识手牌空间、也不对坐标做任何手工补偿；
    /// 牌画在哪一层由宿主换层决定，这里不管。
    /// 队列还是"已打出、等结算"这段成员关系的唯一真源：手牌登记处判忙与回合对账都按它查，别处不再另记一份。</summary>
    public class UI_PlayCardZone : MonoBehaviour
    {
        [Tooltip("排队堆叠的步进（画布像素）：x 为每张牌向右错开的距离，y 为向上错开的距离")] [SerializeField]
        private Vector2 _stackStepPx = new(-115.2f, 0f);

        [Tooltip("队首那张牌的缩放（即刚落下的牌），往后逐张按 _scaleStep 递减")] [SerializeField]
        private float _frontScale = 1.5f;

        [Tooltip("队列中每靠后一位缩小的量：0.1 即每张缩小 10%")] [SerializeField]
        private float _scaleStep = 0.1f;

        [Tooltip("队列缩放下限：牌再多也不小于该值，避免后排看不清")] [SerializeField]
        private float _minScale = 0.5f;

        [Tooltip("入队与队伍前移的补间时长（秒），调大排队更缓慢")] [SerializeField]
        private float _moveDuration = 0.2f;

        private RectTransform _cardFrame; // 卡牌坐标框架（手牌中心节点）：由装配点下发，落点判定与队位都按它换算

        private readonly List<UI_CardHolder> _queue = new();
        private readonly Dictionary<UI_CardHolder, UniTask> _arrivals = new();

        #region 装配：卡牌坐标框架由装配点下发

        public void SetCardFrame(RectTransform frame_) => _cardFrame = frame_;

        #endregion

        #region 落点判定与排队：宿主落点命中后入队，离场时出队并让其余牌依次前移

        // 命中判定只认一个点：调用方传卡牌在卡牌坐标框架里的锚点坐标，换算进本区矩形再判包含；
        // 矩形本身就是可见的有效区，本类不再对坐标做任何补偿
        public bool Accepts(Vector2 anchoredPosition_)
        {
            RectTransform rect = (RectTransform)transform;
            Vector2 local = rect.InverseTransformPoint(_cardFrame.TransformPoint(anchoredPosition_));
            return rect.rect.Contains(local);
        }

        // 队列（按打入先后）：挂出等待结算的成员关系以它为准，手牌登记处判忙与回合对账都向这里查
        public IReadOnlyList<UI_CardHolder> Queue => _queue;

        public bool Contains(UI_CardHolder card_) => _queue.Contains(card_);

        // 入队即刻起补间不等，轮到执行时由宿主用 WaitArrivalAsync 等它停稳
        public void Enqueue(UI_CardHolder card_)
        {
            if (_queue.Contains(card_)) return;
            _queue.Add(card_);
            _arrivals[card_] = MoveToSlotAsync(card_, _queue.Count - 1, _moveDuration);
        }

        public UniTask WaitArrivalAsync(UI_CardHolder card_) =>
            _arrivals.TryGetValue(card_, out UniTask arrival) ? arrival : UniTask.CompletedTask;

        public bool Remove(UI_CardHolder card_)
        {
            if (!_queue.Remove(card_)) return false;
            _arrivals.Remove(card_);

            for (int i = 0; i < _queue.Count; i++)
                MoveToSlotAsync(_queue[i], i, _moveDuration).Forget();
            return true;
        }

        #endregion

        // 队位补间：位置按序号错开、缩放逐张递减；层内先后由入队时置首保证（队首恒在最上），不在这里写。
        // 队位一律从本区矩形中心起算，与命中判定同源，再换算进卡牌坐标框架
        private UniTask MoveToSlotAsync(UI_CardHolder card_, int index_, float duration_)
        {
            Vector2 position = CardFrameCenter() + _stackStepPx * index_;
            float scale = Mathf.Max(_minScale, _frontScale - _scaleStep * index_);
            return UniTask.WhenAll(
                card_.MoveToAsync(position, Quaternion.identity, duration_),
                card_.ScaleToAsync(scale, duration_));
        }

        // 本区矩形中心在卡牌坐标框架里的位置：判定与队位同源，都从这块矩形出发（与自身 pivot 无关）
        private Vector2 CardFrameCenter()
        {
            RectTransform rect = (RectTransform)transform;
            return _cardFrame.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
        }

        // 编辑器里选中本对象即画出这块隐形矩形的边框：它同时是落点区与打出牌的展示区，方便在 Scene 里拖到位
        private void OnDrawGizmosSelected()
        {
            RectTransform rect = (RectTransform)transform;
            Rect area = rect.rect;
            Vector3 leftBottom = rect.TransformPoint(new Vector3(area.xMin, area.yMin));
            Vector3 rightBottom = rect.TransformPoint(new Vector3(area.xMax, area.yMin));
            Vector3 rightTop = rect.TransformPoint(new Vector3(area.xMax, area.yMax));
            Vector3 leftTop = rect.TransformPoint(new Vector3(area.xMin, area.yMax));
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(leftBottom, rightBottom);
            Gizmos.DrawLine(rightBottom, rightTop);
            Gizmos.DrawLine(rightTop, leftTop);
            Gizmos.DrawLine(leftTop, leftBottom);
        }
    }
}
