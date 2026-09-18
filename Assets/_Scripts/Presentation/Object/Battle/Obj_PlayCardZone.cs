using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>出牌区：打出的牌在此按先后排队堆叠——越靠后的牌越小、层序越靠后，结算期间停在队位不动。
    /// 同时充当拖拽落点判定体，需要 `BoxCollider2D` 才能被命中。</summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class Obj_PlayCardZone : MonoBehaviour
    {
        [Tooltip("排队堆叠的步进：x 为每张牌向右错开的世界距离，y 为向上错开的世界距离")] [SerializeField]
        private Vector2 _stackStep = new(-0.16f, 0.05f);

        [Tooltip("队首那张牌的缩放（即刚落下的牌），往后逐张按 _scaleStep 递减")] [SerializeField]
        private float _frontScale = 1.5f;

        [Tooltip("队列中每靠后一位缩小的量：0.1 即每张缩小 10%")] [SerializeField]
        private float _scaleStep = 0.1f;

        [Tooltip("队列缩放下限：牌再多也不小于该值，避免后排看不清")] [SerializeField]
        private float _minScale = 0.5f;

        [Tooltip("队首层序：第 i 张为基准−i，需与手牌、悬停、离场层序互不重叠")] [SerializeField]
        private int _baseOrder = 60;

        [Tooltip("入队与队伍前移的补间时长（秒），调大排队更缓慢")] [SerializeField]
        private float _moveDuration = 0.2f;

        private readonly List<Obj_Card> _queue = new();
        private readonly Dictionary<Obj_Card, UniTask> _arrivals = new();

        private Collider2D _collider;

        private void Awake() => _collider = GetComponent<Collider2D>();

        #region 落点判定与排队：宿主落点命中后入队，离场时出队并让其余牌依次前移

        // 命中判定只认一个点：调用方传卡牌根物体的世界坐标，落在本区碰撞体内即为命中
        public bool Accepts(Vector3 worldPosition_) => _collider.OverlapPoint(worldPosition_);

        // 入队即刻起补间不等，轮到执行时由宿主用 WaitArrivalAsync 等它停稳
        public void Enqueue(Obj_Card card_)
        {
            if (_queue.Contains(card_)) return;
            _queue.Add(card_);
            _arrivals[card_] = MoveToSlotAsync(card_, _queue.Count - 1, _moveDuration);
        }

        public UniTask WaitArrivalAsync(Obj_Card card_) =>
            _arrivals.TryGetValue(card_, out UniTask arrival) ? arrival : UniTask.CompletedTask;

        public bool Remove(Obj_Card card_)
        {
            if (!_queue.Remove(card_)) return false;
            _arrivals.Remove(card_);

            for (int i = 0; i < _queue.Count; i++)
                MoveToSlotAsync(_queue[i], i, _moveDuration).Forget();
            return true;
        }

        #endregion

        // 队位补间：位置按序号错开、缩放逐张递减、层序逐张靠后
        private UniTask MoveToSlotAsync(Obj_Card card_, int index_, float duration_)
        {
            Vector3 position = transform.position + new Vector3(_stackStep.x * index_, _stackStep.y * index_, 0f);
            float scale = Mathf.Max(_minScale, _frontScale - _scaleStep * index_);
            return UniTask.WhenAll(
                card_.MoveToAsync(position, Quaternion.identity, _baseOrder - index_, duration_),
                card_.ScaleToAsync(scale, duration_));
        }
    }
}