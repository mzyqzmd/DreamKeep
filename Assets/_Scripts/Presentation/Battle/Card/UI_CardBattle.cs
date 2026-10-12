using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _DreamKeep.Presentation
{
    /// <summary>战斗卡本体：按卡模型的类型在四张内嵌牌面（攻击 / 技能 / 能力 / 诅咒）之间切换，并把绑定与不可打外观转发给当前那张。
    /// 位置 / 朝向 / 所在层、按下拖动松手、悬停进出也全在这里；牌面只管长什么样，展示卡直接摆牌面、不包这一层，于是天然收不到指针事件。
    /// 位置与朝向一律是**世界姿态**：区域（卡层）只负责挂载与遮挡，槽位由区域按自己的点换算好后下发，本卡只管写到 transform 上；
    /// 换层走 MoveToLayer 唯一通道且一律保世界位置，故各层矩形怎么摆都不会错位。
    /// 能不能抓一律问宿主闸门（非我方回合、还在入场飞行中、已让出牌区等结算的牌都抓不起来），本卡只报事件不判规则；
    /// 悬停不受可打限制：不能打的牌照样抬起来给你看，只是按下去抓不动、拖不出去；
    /// 按下后走哪种交互也由宿主回答：普通卡跟手滑行，AnyEnemy 卡进瞄准模式——卡定住不动，拖动越过启动阈值后交给宿主去驱动目标箭头；
    /// 动画一概不参与判据，滑行合拢中的牌照样能按下即抓（抓起来先 Kill 掉自己的补间）。
    /// 悬停姿态的进入与还原值都由区域按槽位真源给出，本卡不记进入前的瞬时值，补间途中进出也不会写回半截姿态。
    /// 位置与缩放两通道补间重开前先 Kill 同通道，补间按通道独占。</summary>
    public sealed class UI_CardBattle : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("牌面")] [Tooltip("攻击牌面：绑定的卡为攻击类型时切到它")] [SerializeField]
        private UI_CardFace _attackFace;

        [Tooltip("技能牌面：绑定的卡为技能类型时切到它")] [SerializeField]
        private UI_CardFace _skillFace;

        [Tooltip("能力牌面：绑定的卡为能力类型时切到它")] [SerializeField]
        private UI_CardFace _abilityFace;

        [Tooltip("诅咒牌面：绑定的卡为诅咒类型时切到它")] [SerializeField]
        private UI_CardFace _curseFace;

        private UI_CardFace _face; // 当前激活的牌面：Bind 时按卡类型切到对应那张

        [Tooltip("悬停时的缩放目标值（绝对值而非倍率）：手牌基准尺寸为 1，1.15 即比常态放大 15%")] [SerializeField]
        private float _hoverScale = 1.15f;

        [Tooltip(
            "悬停时沿区域的'上'抬起的画布像素：抬高才不会被相邻手牌压住，方向与幅度按所在区域换算成世界向量后下发；上移量不得超过 卡半高 ×（悬停缩放 − 手牌槽位缩放），否则指针擦着卡的下边缘进入时会被甩出卡面、反复进出地抖")]
        [SerializeField]
        private float _hoverLiftPx = 100.8f;

        [Tooltip("悬停进出的补间时长（秒）：抬起、回正、放大与回位共用，比手牌重排的 0.28s 短，进出要跟手")] [SerializeField]
        private float _hoverDuration = 0.12f;

        [Tooltip("瞄准模式的启动阈值（屏幕像素）：按住 AnyEnemy 卡移动超过该距离才把它定住并显示目标箭头，纯点击不触发")] [SerializeField]
        private float _aimThresholdPx = 12f;

        private RectTransform _rect;
        private Tween _moveTween;
        private Tween _scaleTween;
        private bool _dragging;
        private bool _hovered;
        private bool _aimActivated;
        private Vector3 _grabOffset;
        private Vector2 _pressScreenPosition;

        // "已就位"的判定阈值：重排据此跳过空转补间（位置差不足 0.01 世界单位约合 1.4 画布像素，肉眼不可见）
        private const float PoseEpsilon = 0.01f;
        private const float AngleEpsilon = 0.05f;
        private const float ScaleEpsilon = 0.001f;

        public UI_HandView Host { get; private set; }

        public CardModel Card => _face.Card;

        /// <summary>当前世界位置（卡根 pivot＝卡下沿中点）：拖拽跟手、落点判定与悬停抬起都以这个锚点起算。</summary>
        public Vector3 WorldPosition => _rect.position;

        /// <summary>当前世界朝向：起飞抖动等需要原位重写姿态的场合与 WorldPosition 配对使用。</summary>
        public Quaternion WorldRotation => _rect.rotation;

        /// <summary>卡牌矩形的世界中心：目标箭头的起点，卡抬起、缩放、旋转时自动跟随。</summary>
        public Vector3 WorldCenter => _rect.TransformPoint(_rect.rect.center);

        /// <summary>悬停抬起量（画布像素）：由所在区域换算成世界的"上"向量后下发，进入姿态仍归本卡独占。</summary>
        public float HoverLiftPx => _hoverLiftPx;

        /// <summary>位置与朝向是否已等于目标姿态：重排按它跳过空转补间。</summary>
        public bool IsAtPose(Vector3 worldPosition_, Quaternion worldRotation_) =>
            (_rect.position - worldPosition_).sqrMagnitude < PoseEpsilon * PoseEpsilon &&
            Quaternion.Angle(_rect.rotation, worldRotation_) < AngleEpsilon;

        /// <summary>缩放是否已等于目标：重排按它跳过空转补间。</summary>
        public bool IsAtScale(float scale_) => Mathf.Abs(transform.localScale.x - scale_) < ScaleEpsilon;

        private void Awake() => _rect = (RectTransform)transform;

        public void SetHost(UI_HandView host_) => Host = host_;

        #region 选脸与绑定：按卡类型切到对应牌面，再把写入转发给当前那张

        // 切脸：目标脸与当前脸相同就一个字都不写（同类型重绑零 SetActive 抖动）；某个类型漏接引用时报错跳过，不静默半成品
        public void Bind(CardModel card_)
        {
            UI_CardFace face = card_.Type switch
            {
                ECardType.Attack => _attackFace,
                ECardType.Skill => _skillFace,
                ECardType.Ability => _abilityFace,
                ECardType.Curse => _curseFace,
                _ => null,
            };
            if (face == null)
            {
                Debug.LogError($"[UI_CardBattle] 卡「{card_.Name}」的类型 {card_.Type} 没有接上对应牌面，牌面未切换");
                return;
            }

            if (_face != face)
            {
                if (_face) _face.gameObject.SetActive(false);
                face.gameObject.SetActive(true);
                _face = face;
            }

            _face.Bind(card_);
        }

        public void SetPlayableLook(bool playable_) => _face.SetPlayableLook(playable_);

        #endregion

        #region 拖拽：按下先问宿主能不能抓、拖动跟手、松手与中断交宿主处置

        // 按下：UGUI 只会把事件送到指针下最上层那张牌，它不可抓就静默忽略，不穿透到下面那张；无宿主即没被手牌区管，同样静默
        public void OnPointerDown(PointerEventData eventData)
        {
            if (!Host || !Host.CanGrab(this)) return;
            _dragging = true;
            _aimActivated = false;
            _pressScreenPosition = eventData.position;
            _grabOffset = Host.PointerWorldPosition() - WorldPosition;
            Host.OnCardGrabbed(this);
        }

        // 拖动：瞄准模式（AnyEnemy 卡）下卡牌定住不跟手，越过启动阈值后把拖动交给宿主去驱动目标箭头；其余仍是跟手滑行
        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging) return;
            if (Host.IsAiming(this))
            {
                DragAim(eventData);
                return;
            }

            Place(Host.PointerWorldPosition() - _grabOffset, _rect.rotation);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_dragging) return;
            _dragging = false;
            _aimActivated = false;
            Host.OnCardReleased(this); // 瞄准与否由宿主分流：松手后是打出还是回位都不在这里判断
        }

        // 瞄准拖动：指针越过启动阈值那一刻起才通知宿主，此后每帧都通知（箭头随指针刷新）；阈值前什么都不做，故纯点击不留痕迹
        private void DragAim(PointerEventData eventData_)
        {
            if (!_aimActivated)
            {
                if ((eventData_.position - _pressScreenPosition).sqrMagnitude <
                    _aimThresholdPx * _aimThresholdPx) return;
                _aimActivated = true;
            }

            Host.OnAimDragged(this);
        }

        #endregion

        // 拖动中被禁用（回收 / 关闭）时直接落回槽位，不停在半途；悬停状态一并清掉，防实例复用后 EnterHover 被早退挡掉
        private void OnDisable()
        {
            ClearHover();
            _aimActivated = false;
            if (!_dragging) return;
            _dragging = false;
            Host.OnCardDragCancelled(this);
        }

        #region 悬停进出：区域裁定出唯一悬停牌后调用，进入按槽位拎起，退出按槽位真源一次还原

        // 进入：按区域给的槽位姿态拎起——位置取"槽位 + 抬起"、朝向回正到区域的基准朝向（与槽位求解同源），同时放大；
        // 换层已由区域先行完成，这里只管姿态
        public void EnterHover(Vector3 worldPosition_, Quaternion worldRotation_)
        {
            if (_hovered) return;
            _hovered = true;
            MoveToAsync(worldPosition_, worldRotation_, _hoverDuration).Forget();
            ScaleToAsync(transform.localScale.x * _hoverScale, _hoverDuration).Forget();
        }

        // 退出：按区域给的槽位世界姿态一次还原位置、朝向与缩放三项，与进入时的瞬时状态无关，补间途中进出也不会写回半截姿态
        public void ExitHover(Vector3 worldPosition_, Quaternion worldRotation_, float scale_)
        {
            if (!_hovered) return;
            _hovered = false;
            MoveToAsync(worldPosition_, worldRotation_, _hoverDuration).Forget();
            ScaleToAsync(scale_, _hoverDuration).Forget();
        }

        #endregion

        // 离手 / 被拖走 / 入场中等不还原场景：只清状态，把姿态交给后续补间
        public void ClearHover() => _hovered = false;

        #region 落位与层归属：世界姿态与所在层的唯一写入通道

        public void Place(Vector3 worldPosition_, Quaternion worldRotation_)
        {
            _rect.position = worldPosition_;
            _rect.rotation = worldRotation_;
        }

        // 换层：保世界位置重挂（区域只负责挂载与遮挡，矩形怎么摆都不会让牌跳），index_ < 0 表示追加置末，否则插到指定兄弟位；
        // 排位没变时一个字都不写——写兄弟序号会标脏画布触发重建
        public void MoveToLayer(Transform layer_, int index_)
        {
            if (_rect.parent != layer_) _rect.SetParent(layer_, true);
            int target = index_ < 0 ? _rect.parent.childCount - 1 : index_;
            if (_rect.GetSiblingIndex() != target) _rect.SetSiblingIndex(target);
        }

        #endregion

        #region 动效：位置与缩放两通道补间，重开前先 Kill 同通道

        // 滑翔落位：位置与朝向按补间推进，起终点都是世界姿态
        public UniTask MoveToAsync(Vector3 worldPosition_, Quaternion worldRotation_, float duration_)
        {
            _moveTween?.Kill();
            Vector3 fromPosition = _rect.position;
            Quaternion fromRotation = _rect.rotation;
            float progress = 0f;
            _moveTween = DOTween.To(() => progress, value_ => progress = value_, 1f, duration_)
                .SetEase(Ease.OutQuad)
                .OnUpdate(() => Place(Vector3.Lerp(fromPosition, worldPosition_, progress),
                    Quaternion.Slerp(fromRotation, worldRotation_, progress)));
            return _moveTween.WaitAsync();
        }

        public UniTask ScaleToAsync(float scale_, float duration_) =>
            UiTween.PlayScalar(ref _scaleTween, () => transform.localScale.x,
                value_ => transform.localScale = Vector3.one * value_, scale_, duration_).WaitAsync();

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
    }
}