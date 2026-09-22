using _DreamKeep.RuntimeData;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _DreamKeep.Presentation
{
    /// <summary>战斗包装：把一张纯展示牌面（UI_CardFace）包成"战斗手牌里的那张卡"——需要什么类型的卡就包一层什么，包装即卡种。
    /// 位置 / 朝向 / 所在层、命中面、按下拖动松手、悬停进出全在这里；牌面只管长什么样，展示卡直接摆牌面、不包这一层，于是天然收不到指针事件。
    /// 换层由 MoveToLayer 唯一通道下达（各层 rect 与 pivot 完全一致，本地值原样保留即视觉零位移）；
    /// 能不能抓一律问宿主闸门（非我方回合、飞行中 / 已挂出待结算 / 还没全部抽完落稳的牌都抓不起来），包装只报事件不判规则。
    /// 悬停姿态的还原值由宿主按槽位真源给出，包装不记进入前的瞬时值，补间途中进出也不会写回半截姿态。
    /// 位置与缩放两通道补间重开前先 Kill 同通道，补间按通道独占。</summary>
    public class UI_CardHolder : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Tooltip("内嵌的牌面：卡面内容、外框配色与不可打外观都转发给它")] [SerializeField]
        private UI_CardFace _face;

        [Tooltip("悬停时的缩放目标值（绝对值而非倍率）：手牌基准尺寸为 1，1.15 即比常态放大 15%")] [SerializeField]
        private float _hoverScale = 1.15f;

        [Tooltip("悬停时向上抬起的画布像素：抬高才不会被相邻手牌压住；上移量不得超过 卡半高 ×（悬停缩放 − 手牌槽位缩放），否则指针擦着卡的下边缘进入时会被甩出卡面、反复进出地抖（本卡半高 298px、悬停缩放 1.15 时上限约 45px）")] [SerializeField]
        private float _hoverLiftPx = 100.8f;

        [Tooltip("悬停进出的补间时长（秒）：抬起、回正、放大与回位共用，比手牌重排的 0.28s 短，进出要跟手")] [SerializeField]
        private float _hoverDuration = 0.12f;

        private RectTransform _rect;
        private Tween _moveTween;
        private Tween _scaleTween;
        private bool _dragging;
        private bool _hovered;
        private Vector2 _grabOffset;

        public UI_HandView Host { get; private set; }

        public CardModel Card => _face.Card;

        /// <summary>当前锚点位置（相对卡牌容器中心）：悬停拎起与拖拽跟手都以它为起点。</summary>
        public Vector2 AnchoredPosition => _rect.anchoredPosition;

        /// <summary>是否有位置补间在飞：悬停闸门用，飞行中的牌不响应悬停，避免悬停姿态被补间逐帧顶掉。</summary>
        public bool IsMoving => _moveTween != null && _moveTween.IsActive();

        private void Awake() => _rect = (RectTransform)transform;

        public void SetHost(UI_HandView host_) => Host = host_;

        // 牌面转发：卡面内容与外观的写入入口都收在包装上，手牌层只认包装
        public void Bind(CardModel card_) => _face.Bind(card_);

        public void SetPlayableLook(bool playable_) => _face.SetPlayableLook(playable_);

        #region 拖拽：按下先问宿主能不能抓、拖动跟手、松手与中断交宿主处置

        // 按下：UGUI 只会把事件送到指针下最上层那张牌，它不可抓就静默忽略，不穿透到下面那张；无宿主即没被手牌层管，同样静默
        public void OnPointerDown(PointerEventData eventData)
        {
            if (!Host || !Host.CanGrab(this)) return;
            _dragging = true;
            _grabOffset = Host.PointerLocalPosition() - AnchoredPosition;
            Host.OnCardGrabbed(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging) return;
            Place(Host.PointerLocalPosition() - _grabOffset, transform.rotation);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_dragging) return;
            _dragging = false;
            Host.OnCardReleased(this);
        }

        #endregion

        // 拖动中被禁用（回收 / 关闭）时直接落回槽位，不停在半途；悬停状态一并清掉，防实例复用后 EnterHover 被早退挡掉
        private void OnDisable()
        {
            ClearHover();
            if (!_dragging) return;
            _dragging = false;
            Host.OnCardDragCancelled(this);
        }

        #region 悬停进出：宿主裁定出唯一悬停牌后调用，进入拎起，退出按槽位真源原位还原

        // 进入：从当前位置（即槽位）抬起并回正，同时放大；换层已由宿主先行完成，这里只管姿态
        public void EnterHover()
        {
            if (_hovered) return;
            _hovered = true;
            MoveToAsync(AnchoredPosition + Vector2.up * _hoverLiftPx, Quaternion.identity, _hoverDuration).Forget();
            ScaleToAsync(_hoverScale, _hoverDuration).Forget();
        }

        // 退出：按宿主给的槽位位置、朝向与缩放一次还原三项，与进入时的瞬时状态无关，补间途中进出也不会写回半截姿态
        public void ExitHover(Vector2 position_, Quaternion rotation_, float scale_)
        {
            if (!_hovered) return;
            _hovered = false;
            MoveToAsync(position_, rotation_, _hoverDuration).Forget();
            ScaleToAsync(scale_, _hoverDuration).Forget();
        }

        #endregion

        // 离手 / 被拖走 / 入场中等不还原场景：只清状态，把姿态交给后续补间
        public void ClearHover() => _hovered = false;

        #region 落位与层归属：设置锚点坐标与朝向、换到所属层并按层内兄弟位排位，位置与层归属的唯一写入通道

        public void Place(Vector2 anchoredPosition_, Quaternion rotation_)
        {
            _rect.anchoredPosition = anchoredPosition_;
            _rect.localRotation = rotation_;
        }

        // 换层：父节点不同才重挂（各层 rect 与 pivot 完全一致，本地值原样保留即视觉零位移）；
        // index_ < 0 表示追加置末，否则插到指定兄弟位；排位没变时一个字都不写——写兄弟序号会标脏画布触发重建
        public void MoveToLayer(Transform layer_, int index_)
        {
            if (_rect.parent != layer_) _rect.SetParent(layer_, false);
            int target = index_ < 0 ? _rect.parent.childCount - 1 : index_;
            if (_rect.GetSiblingIndex() != target) _rect.SetSiblingIndex(target);
        }

        #endregion

        #region 动效：位置与缩放两通道补间，重开前先 Kill 同通道

        // 滑翔落位：位置与朝向按补间推进
        public UniTask MoveToAsync(Vector2 anchoredPosition_, Quaternion rotation_, float duration_)
        {
            _moveTween?.Kill();
            Vector2 fromPosition = _rect.anchoredPosition;
            Quaternion fromRotation = _rect.localRotation;
            float progress = 0f;
            _moveTween = DOTween.To(() => progress, value_ => progress = value_, 1f, duration_)
                .SetEase(Ease.OutQuad)
                .OnUpdate(() => Place(Vector2.Lerp(fromPosition, anchoredPosition_, progress),
                    Quaternion.Slerp(fromRotation, rotation_, progress)));
            return WaitAsync(_moveTween);
        }

        public UniTask ScaleToAsync(float scale_, float duration_)
        {
            _scaleTween?.Kill();
            _scaleTween = DOTween.To(() => transform.localScale.x,
                    value_ => transform.localScale = Vector3.one * value_, scale_, duration_)
                .SetEase(Ease.OutQuad);
            return WaitAsync(_scaleTween);
        }

        #endregion

        #region 清干净播放的动画

        // 回收前统一清干净补间，防止补间继续写已被复用的对象
        public void KillMotion()
        {
            _moveTween?.Kill();
            _moveTween = null;
            _scaleTween?.Kill();
            _scaleTween = null;
        }

        #endregion

        #region DOTween → UniTask 等待胶水
        // 完成与被 Kill 都算结束，杜绝 await 悬挂（第二个消费者出现时再抽公共扩展）
        private static UniTask WaitAsync(Tween tween_)
        {
            if (!tween_.IsActive()) return UniTask.CompletedTask;
            UniTaskCompletionSource source = new();
            tween_.OnComplete(() => source.TrySetResult()).OnKill(() => source.TrySetResult());
            return source.Task;
        }

        #endregion
    }
}
