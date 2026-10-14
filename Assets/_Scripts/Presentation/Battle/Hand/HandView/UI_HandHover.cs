using System.Collections.Generic;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>手牌悬停器：与手牌区本体同挂一个节点，每帧由本体的 Update 驱动。一次射线裁决出指针下可悬停的那张牌，
    /// 演出它的抬起与退出、两侧行内牌的避让，并把它的词条说明对账到弹层列——悬停转拖拽是同一张牌，拖拽中照常显示、随卡走。
    /// 悬停身份与避让量都长在这里：本体排布时来问（IsHovered / DodgeOf），牌落位与被回收时来通知。</summary>
    public sealed class UI_HandHover : MonoBehaviour
    {
        [Tooltip("悬停避让量（画布像素）：悬停某张牌时，它左侧的行内牌左移、右侧的右移各该距离，抬起的牌便不再挡住右邻；0 即不避让")] [SerializeField]
        private float _hoverDodgePx = 160f;

        private UI_HandView _handView;
        private UI_PopTextRoot _popTextRoot;

        // 悬停会话：当前悬停的那张牌、挂着词条气泡列的牌与它这一份内容（换牌才重建，并把整列重挂到新牌名下）
        private UI_CardBattle _hoveredCard;
        private UI_CardBattle _tipCard;
        private readonly List<(string Name, string Description)> _affixTips = new();

        // 逐帧射线裁决的复用件：命中列表与指针数据只建一次，避免每帧产生垃圾
        private readonly List<RaycastResult> _raycastResults = new();
        private PointerEventData _pointerData;
        private EventSystem _pointerEventSystem;

        // 生命周期：本体由 RequireComponent 保证同节点必有
        private void Awake() => _handView = GetComponent<UI_HandView>();

        // 装配：气泡列的宿主（注入时机早于首次驱动，无碍）
        [Inject]
        public void Construct(UI_PopTextRoot popTextRoot_) => _popTextRoot = popTextRoot_;

        #region 对外入口：由本体每帧驱动与查询

        // 每帧裁定一次悬停：指针下没有可交互的牌时目标为 null，等价于退出；次序是"先定目标 → 撤旧 → 写新"。
        // 进入姿态一律以槽位为基准（槽位位置 + 沿点朝上的抬起、朝向回正到点的朝向），故滑到一半被悬停也不会停在半途
        internal void TickHover()
        {
            UI_CardBattle current = _hoveredCard;
            UI_CardBattle target = RaycastTopCard();
            if (target == current) return;

            _hoveredCard = target; // 先落新悬停身份：撤旧与避让都按它算
            if (current) ExitHover(current);
            if (!target)
            {
                ApplyHoverDodge(); // 悬停撤离：已无悬停者，两侧避让原路收回
                return;
            }

            int index = _handView.RowIndexOf(target);
            _handView.EvaluateSlot(index, _handView.RowCount(), 0f, out Vector3 position, out _, out _);
            _handView.MoveToFocus(target); // 悬停期间位置、朝向、缩放与所在层整段交给包装的悬停补间，这里只定基准
            target.EnterHover(position + _handView.FanCenterPoint.TransformVector(Vector3.up * target.HoverLiftPx),
                _handView.FanCenterPoint.rotation);
            ApplyHoverDodge();
        }

        // 词条说明气泡列的对账：正在拖拽的那张牌优先，其次正在悬停的那张——它身上"不展示描述"的词条在卡片右下角外侧向上叠列说明。
        // 悬停转拖拽是同一张牌，判据早退不重建，故拖拽与瞄准全程照常显示、随卡移动（玩家决定打不打时正需要对照关键词）；
        // 整列挂在牌自己名下，随卡抬起、缩放、换层与拖动自动跟随，故同一张牌一次写入即够（不再每帧写位置）；
        // 换牌才重建内容并把整列重挂过去，指针离开、松手受理（此后牌交给出牌区排队与结算）、牌离手都落到这里收起
        internal void TickAffixTips()
        {
            UI_CardBattle card = _handView.DraggedCard ? _handView.DraggedCard : _hoveredCard;
            if (card == _tipCard) return;

            if (_tipCard) _popTextRoot.HideTips(_tipCard);
            _tipCard = card;
            if (!card) return;

            _affixTips.Clear();
            foreach ((string text, CardAffix affix) in card.Card.GetDescriptionLines())
                if (affix != null) _affixTips.Add((text, affix.GetDescription()));
            if (_affixTips.Count > 0) _popTextRoot.ShowTips(card, _affixTips, card.AffixTipAnchor, EUiAnchor.BottomLeft, card.transform);
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
