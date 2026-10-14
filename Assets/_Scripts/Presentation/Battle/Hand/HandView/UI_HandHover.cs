using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace _DreamKeep.Presentation
{
    /// <summary>手牌悬停器：与手牌区本体同挂一个节点，每帧由本体的 Update 驱动。一次射线裁决出指针下可悬停的那张牌，
    /// 演出它的对齐与退出、两侧行内牌的避让，并把它的词条说明对账到卡自己的气泡发起器——悬停转拖拽是同一张牌，拖拽中照常显示、随卡走。
    /// 卡的"悬停"只能由这里裁决（叠牌下事件驱动会抖、瞄准中指针离开卡但气泡要留），故卡上的气泡发起器是外部驱动模式，显隐一律由本处调。
    /// 悬停身份与避让量都长在这里：本体排布时来问（IsHovered / DodgeOf），牌落位与被回收时来通知。</summary>
    public sealed class UI_HandHover : MonoBehaviour
    {
        [Tooltip("悬停避让量（画布像素）：悬停某张牌时，它左侧的行内牌左移、右侧的右移各该距离，抬起的牌便不再挡住右邻；0 即不避让")] [SerializeField]
        private float _hoverDodgePx = 160f;

        [Tooltip("悬停撤销宽限（秒）：刚进入悬停的滑入期间指针可能短暂脱卡（卡还在滑），这段时间内不撤销；抓起拖拽与移向别的牌不受影响")] [SerializeField]
        private float _hoverGraceDuration = 0.15f;

        private UI_HandView _handView;

        // 悬停会话：当前悬停的那张牌、挂着词条气泡列的牌
        private UI_CardBattle _hoveredCard;
        private float _hoverGraceUntil; // 撤销宽限的截止时刻：滑入期间指针短暂脱卡也不撤销
        private UI_CardBattle _tipCard;

        // 逐帧射线裁决的复用件：命中列表与指针数据只建一次，避免每帧产生垃圾
        private readonly List<RaycastResult> _raycastResults = new();
        private PointerEventData _pointerData;
        private EventSystem _pointerEventSystem;

        // 生命周期：本体由 RequireComponent 保证同节点必有
        private void Awake() => _handView = GetComponent<UI_HandView>();

        #region 对外入口：由本体每帧驱动与查询

        // 每帧裁定一次悬停：指针下没有可交互的牌时目标为 null，等价于退出；次序是"先定目标 → 撤旧 → 写新"。
        // 进入姿态一律把"卡锚点上方该距离的点"对齐到指针（朝向回正到点的朝向），指针遂停在卡内该高度，
        // 故从卡边缘进入也不会因卡自身移动被甩出卡面；只对齐一次，之后指针在卡内移动卡不动
        internal void TickHover()
        {
            UI_CardBattle current = _hoveredCard;
            UI_CardBattle target = RaycastTopCard();
            if (target == current) return;
            // 撤销宽限：刚进入悬停的滑入期间卡还在滑，指针可能短暂脱卡——这段时间内不撤销；
            // 抓起拖拽（此后归拖拽跟随接管）与命中另一张可悬停牌都不受宽限影响
            if (!target && current && !_handView.DraggedCard && Time.time < _hoverGraceUntil) return;

            _hoveredCard = target; // 先落新悬停身份：撤旧与避让都按它算
            if (current) ExitHover(current);
            if (!target)
            {
                ApplyHoverDodge(); // 悬停撤离：已无悬停者，两侧避让原路收回
                return;
            }

            _handView.MoveToFocus(target); // 悬停期间位置、朝向、缩放与所在层整段交给包装的悬停补间，这里只定基准
            target.EnterHover(_handView.PointerAnchorPosition(target), _handView.FanCenterPoint.rotation);
            _hoverGraceUntil = Time.time + _hoverGraceDuration;
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
            if (_hoveredCard == view_) _hoveredCard = null;
        }

        #endregion

        // 射线裁决：按命中先后取第一张可悬停的牌；同一张卡可能被多个图形报出，只判一次。
        // 当前悬停中的牌照常作为候选参与（指针还停在它身上就是"目标不变"），因此它的进出补间不会把自己顶掉
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

            _pointerData.position = Pointer.current.position.ReadValue();
            _raycastResults.Clear();
            eventSystem.RaycastAll(_pointerData, _raycastResults);

            UI_CardBattle previous = null;
            foreach (RaycastResult result in _raycastResults)
            {
                UI_CardBattle card = result.gameObject.GetComponentInParent<UI_CardBattle>();
                if (card == previous) continue; // 同一张卡可能被多个图形报出，只判一次
                previous = card;
                if (!card) continue;
                if (_handView.CanHover(card)) return card;
            }

            return null;
        }

        // 撤下悬停：牌还在行里、没被拖拽或让位时，换回手牌层并按当前槽位一次还原位置、朝向与缩放；
        // 其余情况（离手 / 被拖走 / 已让位等结算）只清状态，交给后续补间
        private void ExitHover(UI_CardBattle card_)
        {
            if (!_handView.CanHover(card_))
            {
                card_.ClearHover();
                return;
            }

            int index = _handView.RowIndexOf(card_);
            _handView.MoveToHand(card_);
            _handView.EvaluateSlot(index, _handView.RowCount(), DodgeOf(index), out Vector3 position,
                out Quaternion rotation, out float scale);
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
                if (!_handView.IsInRow(card)) continue;

                int index = _handView.RowIndexOf(card);
                _handView.EvaluateSlot(index, _handView.RowCount(), DodgeOf(index), out Vector3 position,
                    out Quaternion rotation, out _);
                card.MoveToAsync(position, rotation, card.HoverDuration).Forget();
            }
        }
    }
}
