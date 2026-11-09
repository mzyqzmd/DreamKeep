using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.EngineUtility;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _DreamKeep.Presentation
{
    /// <summary>手牌悬停器：与手牌区本体同挂一个节点，每帧由本体的 Update 驱动。一次射线裁决出指针下可悬停的那张牌，
    /// 演出它的原位偏移（卡留在自己槽位上方、只做偏移 + 放大，不跟随指针）与退出、两侧行内牌的避让，
    /// 并把它的词条说明对账到卡自己的气泡发起器——悬停转拖拽是同一张牌，拖拽中照常显示、随卡走。
    /// 卡的"悬停"只能由这里裁决（叠牌下事件驱动会抖、瞄准中指针离开卡但气泡要留），故卡上的气泡发起器与两个姿态组件都是被驱动模式，一律由本处调。
    /// 命中是并集：射线命中偏移后的牌即算命中，射线落空但指针仍在进入时的基座矩形内也保持——卡被偏移移开后不会因脱卡抖动。
    /// 悬停身份与避让量都长在这里：本体排布时来问（IsHovered / DodgeOf），牌落位与被回收时来通知。</summary>
    public sealed class UI_HandHover : MonoBehaviour
    {
        [Tooltip("悬停避让量（画布像素）：悬停某张牌时，它左侧的行内牌左移、右侧的右移各该距离，偏移后的牌便不再挡住右邻；0 即不避让")] [SerializeField]
        private float _hoverDodgePx = 160f;

        private UI_HandView _handView;

        // 悬停会话：当前悬停的那张牌、挂着词条气泡列的牌
        private UI_CardBattle _hoveredCard;
        private UI_CardBattle _tipCard;

        // 进入悬停那一刻的基座矩形（原点 / 朝向 / 世界尺寸 / pivot）：卡被偏移移走后，指针还落在原位置内也算悬停
        private Vector3 _basePosition;
        private Quaternion _baseRotation;
        private Vector2 _baseSize;
        private Vector2 _basePivot;
        private bool _hasBaseRect;

        // 逐帧射线裁决的复用件：命中列表与指针数据只建一次，避免每帧产生垃圾
        private readonly List<RaycastResult> _raycastResults = new();
        private PointerEventData _pointerData;
        private EventSystem _pointerEventSystem;

        // 生命周期：本体由 RequireComponent 保证同节点必有
        private void Awake() => _handView = GetComponent<UI_HandView>();

        #region 对外入口：由本体每帧驱动与查询

        // 每帧裁定一次悬停：指针下没有可交互的牌时目标为 null，等价于退出；次序是"先定目标 → 撤旧 → 写新"。
        // 命中是并集：射线命中偏移后的牌即算命中；射线落空时，指针仍落在进入时的基座矩形内也保持（原位置照样算悬停）；
        // 拖拽中不靠并集保持（此后归拖拽跟随接管）。
        // 进入姿态取卡自己的槽位偏移位（原位偏移、朝向回正：不读指针），鼠标横移不会把它带走
        internal void TickHover()
        {
            UI_CardBattle current = _hoveredCard;
            UI_CardBattle target = RaycastTopCard();
            if (!target && current && !_handView.DraggedCard && PointerInBaseRect()) target = current;
            if (target == current) return;

            // 进入前先按槽位真源取偏移位：取不到（不在行内）就当作没命中，静默
            Vector3 position = default;
            Quaternion rotation = default;
            if (target && !TryGetRaisedPose(target, out position, out rotation)) target = null;

            _hoveredCard = target; // 先落新悬停身份：撤旧与避让都按它算
            if (current) ExitHover(current);
            if (!target)
            {
                ApplyHoverDodge(); // 悬停撤离：已无悬停者，两侧避让原路收回
                return;
            }

            CacheBaseRect(target);
            _handView.MoveToFocus(target); // 悬停期间位置、朝向、缩放与所在层整段交给卡的悬停组件，这里只定基准
            target.EnterHover(position, rotation);
            ApplyHoverDodge();
        }

        // 词条说明气泡列的显隐对账：正在拖拽的那张牌优先，其次正在悬停的那张；内容在卡牌 Bind 时已随绑定写入它的气泡发起器，
        // 这里只管"该由谁显示"——换牌才切换一次，故悬停转拖拽是同一张牌时不重建、拖拽与瞄准全程照常显示并随卡移动
        // （玩家决定打不打时正需要对照关键词）；指针离开、松手受理（此后牌交给出牌区排队与结算）、牌离手都落到这里收起
        internal void TickAffixTips()
        {
            UI_CardBattle card = _handView.DraggedCard ? _handView.DraggedCard : _hoveredCard;
            if (card == _tipCard) return;

            if (_tipCard) _tipCard.HoverTip.Hide();
            _tipCard = card;
            if (card) card.HoverTip.Show(); // 无词条说明的牌内容为空，Show 内部早退
        }

        // 让出悬停身份：切到不可打等状态时由本体调用，只清状态与身份，姿态一律交给后续补间
        internal void ClearHover()
        {
            if (!_hoveredCard) return;
            _hoveredCard.ClearHover();
            _hoveredCard = null;
            _hasBaseRect = false;
            ApplyHoverDodge(); // 悬停被外力撤下：两侧避让先收回（已无悬停者，各牌避让量恒 0），悬停牌自身仍交给后续重排
        }

        /// <summary>这张牌此刻是不是悬停牌：本体排布的跳过条件用它。</summary>
        internal bool IsHovered(UI_CardBattle card_) => _hoveredCard == card_;

        /// <summary>该行内序号的牌此刻的避让量：悬停牌左侧左移、右侧右移各一档，悬停牌本身与无悬停时为 0。</summary>
        internal float DodgeOf(int index_)
        {
            if (_hoverDodgePx == 0f || !_hoveredCard) return 0f;
            int hovered = _handView.RowIndexOf(_hoveredCard);
            if (hovered < 0) return 0f;
            return index_ > hovered ? _hoverDodgePx : index_ < hovered ? -_hoverDodgePx : 0f;
        }

        /// <summary>一张牌入场结束落位：若此刻正有牌悬停着，顺手把它纳入避让（两侧的行早已让开）。</summary>
        internal void OnCardSettled()
        {
            if (_hoveredCard) ApplyHoverDodge();
        }

        /// <summary>一张牌被回收进池：它若正是悬停牌则撤下身份，防实例复用后残留旧状态。</summary>
        internal void OnViewReleased(UI_CardBattle view_)
        {
            if (_hoveredCard != view_) return;
            _hoveredCard = null;
            _hasBaseRect = false;
        }

        #endregion

        // 射线裁决：只认指针下最顶层的那个 UI 元素——属于手牌才悬停，被任何可射线弹层（如牌堆查看面板的遮罩）
        // 挡住即当作没有悬停，绝不越过遮挡去找后面的牌。指针落在卡面上即算命中（卡下沿不再留死区），
        // 偏移后指针脱卡的情形由基座矩形并集兜住
        private UI_CardBattle RaycastTopCard()
        {
            if (_handView.Hand.Count == 0) return null; // 手牌空时指针下不可能有可悬停的牌，这次射线直接省掉
            EventSystem eventSystem = EventSystem.current;
            if (!eventSystem) return null;

            if (_pointerEventSystem != eventSystem)
            {
                _pointerEventSystem = eventSystem;
                _pointerData = new PointerEventData(eventSystem);
            }

            if (!PointerInput.TryGetScreenPosition(out Vector2 pointerScreen)) return null;
            _pointerData.position = pointerScreen;
            _raycastResults.Clear();
            eventSystem.RaycastAll(_pointerData, _raycastResults);
            if (_raycastResults.Count == 0) return null;

            UI_CardBattle card = _raycastResults[0].gameObject.GetComponentInParent<UI_CardBattle>();
            return card && _handView.CanHover(card) ? card : null;
        }

        // 槽位姿态：按卡的行内序号取槽位（可叠加悬停避让量），取不到（不在行内）即 false——
        // 悬停的进入、退出与让位三条路共用这一处换算
        private bool TryGetSlotPose(UI_CardBattle card_, float dodgeX_, out Vector3 worldPosition_,
            out Quaternion worldRotation_, out float scale_)
        {
            int index = _handView.RowIndexOf(card_);
            if (index < 0)
            {
                worldPosition_ = default;
                worldRotation_ = default;
                scale_ = 1f;
                return false;
            }

            _handView.EvaluateSlot(index, _handView.RowCount(), dodgeX_, out worldPosition_, out worldRotation_, out scale_);
            return true;
        }

        // 槽位偏移位：按卡自己的槽位（不含悬停避让——成为悬停牌后它自己的避让量恒 0）沿手牌区参考系加上卡自己的偏移量，
        // 朝向回正到手牌区的基准朝向（浅弧的倾角不带进来）；一律读槽位真源、与指针在哪里无关，故鼠标横移不会把卡带走
        private bool TryGetRaisedPose(UI_CardBattle card_, out Vector3 worldPosition_, out Quaternion worldRotation_)
        {
            worldRotation_ = _handView.FanCenterPoint.rotation;
            if (!TryGetSlotPose(card_, 0f, out worldPosition_, out _, out _)) return false;
            Vector2 offset = card_.HoverOffsetPx;
            worldPosition_ += _handView.FanCenterPoint.TransformVector(new Vector3(offset.x, offset.y, 0f));
            return true;
        }

        // 基座矩形：进入悬停那一刻的槽位位姿与卡面世界尺寸——偏移把卡移开后，原位置仍按它算命中
        private void CacheBaseRect(UI_CardBattle card_)
        {
            _hasBaseRect = TryGetSlotPose(card_, 0f, out _basePosition, out _baseRotation, out _);
            if (!_hasBaseRect) return;
            RectTransform rect = (RectTransform)card_.transform;
            _baseSize = Vector2.Scale(rect.rect.size, rect.lossyScale);
            _basePivot = rect.pivot;
        }

        // 并集的那一半：指针是否仍落在悬停牌的基座矩形内（画布平面换算与槽位同一口径）
        private bool PointerInBaseRect() =>
            _hasBaseRect &&
            UiGeometry.Contains(_handView.PointerWorldPosition(), _basePosition, _baseRotation, _baseSize, _basePivot);

        // 撤下悬停：牌还在行里、没被拖拽或让位时，换回手牌层并按当前槽位一次还原位置、朝向与缩放；
        // 其余情况（离手 / 被拖走 / 已让位等结算）只清状态，交给后续补间
        private void ExitHover(UI_CardBattle card_)
        {
            if (!_handView.CanHover(card_))
            {
                card_.ClearHover();
                return;
            }

            _handView.MoveToHand(card_);
            if (!TryGetSlotPose(card_, DodgeOf(_handView.RowIndexOf(card_)), out Vector3 position,
                    out Quaternion rotation, out float scale)) return;
            card_.ExitHover(position, rotation, scale);
        }

        // 悬停避让：以悬停牌为界，左半行内牌左移、右半右移各一档（无悬停者时各牌避让量恒 0，原路归位）；
        // 避让量口径与 DodgeOf 同一处（行内序号现算，悬停牌自身不进循环）；补间与悬停进出同时长——避让是悬停演出的一部分，要跟手。
        // 只挪位置，不碰缩放与层归属；同样只声明目标，"要不要重开补间"由卡片按已声明的目标判（与手牌重排共用同一套通道）
        private void ApplyHoverDodge()
        {
            IReadOnlyList<UI_CardBattle> hand = _handView.Hand;
            for (int i = 0; i < hand.Count; i++)
            {
                UI_CardBattle card = hand[i];
                if (card == _hoveredCard || card == _handView.DraggedCard || _handView.IsEntering(card)) continue;

                // 不在行里（已让位等结算 / 已离手）的牌不参与让位，槽位也取不到
                if (!TryGetSlotPose(card, DodgeOf(_handView.RowIndexOf(card)), out Vector3 position,
                        out Quaternion rotation, out _)) continue;
                card.MoveToAsync(position, rotation, card.HoverDuration).Forget();
            }
        }
    }
}
