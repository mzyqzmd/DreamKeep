using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>出牌区：一块自洽的区域——打出的牌在此按先后排队堆叠，区域自己管命中判定、队列状态与队位落点，
    /// 手牌区只单向问它"在队里吗 / 命中吗 / 收下 / 还回 / 等停稳"，它不认识手牌区，也不判断任何规则。
    /// 它是与手牌区平级的隐形矩形：不挂图形、不参与指针命中，只回答"卡牌锚点（卡根 pivot＝卡下沿中点）落点在不在本区矩形内"这一个问题。
    /// 落点与队位分家：矩形只管判定，队位一律交给区域里的点（CardQueuePoint）——位置写成 点.TransformPoint(画布像素偏移)，
    /// 朝向取点的朝向，故在 Scene 里拖那个点即整队挪位，矩形怎么摆都不影响队位。
    /// 队列还是"已打出、等结算"这段成员关系的唯一真源：手牌区的忙碌判据、回合对账都向它查，别处不再另记一份；
    /// 收下即换层到本区名下并按入队先后排位，队首（最早打出、最先结算的那张）恒在最上。</summary>
    public class UI_PlayCardZone : MonoBehaviour
    {
        [Tooltip("排队堆叠的步进（画布像素）：以队列点为准，x 为每张牌向右错开的距离，y 为向上错开的距离")] [SerializeField]
        private Vector2 _stackStepPx = new(-110f, 0f);

        [Tooltip("队首那张牌的缩放（即刚落下的牌），往后逐张按 _scaleStep 递减")] [SerializeField]
        private float _frontScale = 1.5f;

        [Tooltip("队列中每靠后一位缩小的量：0.1 即每张缩小 10%")] [SerializeField]
        private float _scaleStep = 0.1f;

        [Tooltip("队列缩放下限：牌再多也不小于该值，避免后排看不清")] [SerializeField]
        private float _minScale = 0.5f;

        [Tooltip("入队与队伍前移的补间时长（秒），调大排队更缓慢")] [SerializeField]
        private float _moveDuration = 0.2f;

        [Tooltip("队列落位基准点：队位一律从它起算，在场景里拖它即整队挪位；队列牌挂在本区名下，它只当基准")] [SerializeField]
        private RectTransform _queuePoint;

        private readonly List<UI_CardHolder> _queue = new();
        private readonly Dictionary<UI_CardHolder, UniTask> _arrivals = new();

        #region 落点判定：卡牌锚点落在本区矩形内即算打出

        // 命中判定只认一个点：调用方传卡牌锚点（卡根 pivot＝卡下沿中点）的世界坐标，换算进本区矩形再判包含；
        // 矩形本身就是可见的有效区，本类不再对坐标做任何补偿
        public bool Accepts(Vector3 anchorWorld_)
        {
            RectTransform rect = (RectTransform)transform;
            return rect.rect.Contains(rect.InverseTransformPoint(anchorWorld_));
        }

        #endregion

        #region 排队：收下、让位、等待与交出

        // 队列（按打入先后）：挂出等待结算的成员关系以它为准，手牌区判忙与回合对账都向这里查
        public IReadOnlyList<UI_CardHolder> Queue => _queue;

        public bool Contains(UI_CardHolder card_) => _queue.Contains(card_);

        // 收下：换到本区名下并按入队先后排位（最新入队的置首，即排在层内最下），随即起补间落位、不等它停稳——
        // 轮到执行时由调用方用 WaitArrivalAsync 等它
        public void Enqueue(UI_CardHolder card_)
        {
            if (_queue.Contains(card_)) return;
            _queue.Add(card_);
            card_.MoveToLayer(transform, 0);
            _arrivals[card_] = MoveToSlotAsync(card_, _queue.Count - 1, _moveDuration);
        }

        public UniTask WaitArrivalAsync(UI_CardHolder card_) =>
            _arrivals.TryGetValue(card_, out UniTask arrival) ? arrival : UniTask.CompletedTask;

        // 交出：出队并让其余牌依次前移；怎么飞回手牌由调用方驱动，本区只管让它离开
        public bool Remove(UI_CardHolder card_)
        {
            if (!_queue.Remove(card_)) return false;
            _arrivals.Remove(card_);

            for (int i = 0; i < _queue.Count; i++)
                MoveToSlotAsync(_queue[i], i, _moveDuration).Forget();
            return true;
        }

        #endregion

        // 队位补间：位置从队列点起算、朝向取队列点的朝向、缩放逐张递减；层内先后由收下时置首保证（队首恒在最上），不在这里写
        private UniTask MoveToSlotAsync(UI_CardHolder card_, int index_, float duration_)
        {
            Vector3 position = _queuePoint.TransformPoint(_stackStepPx * index_);
            float scale = Mathf.Max(_minScale, _frontScale - _scaleStep * index_);
            return UniTask.WhenAll(
                card_.MoveToAsync(position, _queuePoint.rotation, duration_),
                card_.ScaleToAsync(scale, duration_));
        }
    }
}
