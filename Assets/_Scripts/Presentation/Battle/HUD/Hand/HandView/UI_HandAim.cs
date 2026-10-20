using System.Collections.Generic;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>手牌瞄准器：与手牌区本体同挂一个节点，瞄准态整体长在这里——目标箭头、瞄准身份、瞄准结果缓存
    ///（目标槽位与目标集合）以及整手牌的数值预览对账。它没有 Update：瞄准结果由单位侧的目标提示驱动每帧算完后
    /// 经宿主一次性下发并顺手驱动本类，故箭头命中色与卡面数值用的永远是当帧刚算出的结果，
    /// 不再有"两个组件谁先 Update"的次序之争。它只认识宿主手牌区与箭头，目标是谁一律由驱动给。</summary>
    public sealed class UI_HandAim : MonoBehaviour
    {
        private const int NoAimSlot = -1; // 尚未指向任何敌人：松手时据此取消，不打出

        private UI_HandView _handView;
        private UI_CardArrow _cardArrow;
        private BattleQueryDispatcher _queryDispatcher;

        // 瞄准状态：正在瞄准的那张牌、箭头是否已展开
        private UI_CardBattle _aimingCard;
        private bool _aimingActive;

        // 瞄准结果的两个同步缓存：目标槽位（存活敌人列表下标）与目标集合，由驱动每帧一次性下发（ApplyResult）。
        // 本类只读不写、不自行推导——松手当帧必须能同步读到槽位，故不能等驱动的下一次下发；
        // 瞄准卡一按下先清成"未指向"，防极快操作下默认值被当成有效目标打出去
        private int _aimSlot = NoAimSlot;
        private IReadOnlyList<Creature> _previewTargets;

        // 生命周期：本体由 RequireComponent 保证同节点必有；箭头是手牌区里的固定子节点，
        // 自愈取值免去 Inspector 接线（字段从本体迁来时不会丢引用）
        private void Awake()
        {
            _handView = GetComponent<UI_HandView>();
            _cardArrow = GetComponentInChildren<UI_CardArrow>(true);
        }

        // 装配：数值预览要经上行门面现算最终值；瞄准本身不需要命令门面
        [Inject]
        public void Construct(BattleQueryDispatcher queryDispatcher_) => _queryDispatcher = queryDispatcher_;

        #region 每帧驱动：由目标提示驱动在下发瞄准结果时调用

        /// <summary>接收本轮瞄准结果并立刻消费：写入两个缓存后同帧刷新兜底取消、箭头位姿与颜色、整手牌的数值预览。
        /// 写入与消费绑在同一次调用里，驱动不可能只下发不刷新，消费方也就不必自己再找时机。</summary>
        internal void ApplyResult(int slot_, IReadOnlyList<Creature> targets_)
        {
            _aimSlot = slot_;
            _previewTargets = targets_;
            Tick();
        }

        #endregion

        #region 瞄准入口：由宿主手牌区在指针事件与状态切换时转发

        // 抓起：瞄准卡在此登记进瞄准模式——松手分流与箭头生命周期以它为准；同时把目标槽位清成"未指向"
        internal void OnGrabbed(UI_CardBattle card_)
        {
            _aimingCard = card_.Card.IsAnyEnemyTarget ? card_ : null;
            _aimingActive = false;
            if (_aimingCard != null) _aimSlot = NoAimSlot;
        }

        // 瞄准拖动：越过启动阈值后由包装逐帧上报，首次上报即展开箭头；此后位姿与颜色由每帧驱动刷新
        internal void OnDragged(UI_CardBattle card_)
        {
            if (card_ != _aimingCard) return;
            _aimingActive = true;
            _cardArrow.Show(card_.WorldCenter, _handView.PointerWorldPosition(), _aimSlot >= 0);
        }

        // 松手：收箭头与瞄准身份，并把当帧的槽位交回宿主——瞄准卡以槽位 ≥ 0 判定打出，普通卡不看这个值
        internal bool Release(UI_CardBattle card_, out int aimSlot_)
        {
            aimSlot_ = _aimSlot;
            bool aiming = card_ == _aimingCard;
            if (aiming) EndAim();
            return aiming;
        }

        // 拖拽被强制中断（包装禁用或回收）：是瞄准卡才收箭头与身份，牌的回位由宿主处置
        internal void Cancel(UI_CardBattle card_)
        {
            if (card_ == _aimingCard) EndAim();
        }

        // 兜底取消：牌已离手、不再被按住或本回合不再可打时收箭头清身份，并把那张牌瞬移回槽位（不播补间）
        internal void CancelActive()
        {
            if (_aimingCard == null) return;
            UI_CardBattle card = _aimingCard;
            EndAim();
            _handView.DropAimDrag(card);
        }

        #endregion

        // 每帧：先兜底校正瞄准状态（牌已离手或不再被按住就取消），箭头展开期间刷新位姿与颜色，最后对账整手牌数值预览
        private void Tick()
        {
            if (_aimingCard != null)
            {
                if (_handView.DraggedCard != _aimingCard || !_handView.IsInHand(_aimingCard))
                {
                    CancelActive();
                }
                else if (_aimingActive)
                {
                    _cardArrow.Show(_aimingCard.WorldCenter, _handView.PointerWorldPosition(), _aimSlot >= 0);
                }
            }

            RefreshPreviews();
        }

        // 数值预览的逐帧对账：整手每帧对账——平时只含来源侧修正（力量/虚弱），正在瞄准的那张叠加目标集合的目标侧修正
        //（卡视图内部按"各目标的最终值是否一致"折叠，未变的卡在卡视图内部差分早退、零字符串构建）
        private void RefreshPreviews()
        {
            UI_CardBattle aimed = _handView.AimedCard;
            IReadOnlyList<UI_CardBattle> hand = _handView.Hand;
            for (int i = 0; i < hand.Count; i++)
            {
                UI_CardBattle card = hand[i];
                card.RefreshPreview(_queryDispatcher, card == aimed ? _previewTargets : null);
            }
        }

        // 收瞄准：清身份并收起箭头（箭头对"从未展开"是空操作）
        private void EndAim()
        {
            _aimingCard = null;
            _aimingActive = false;
            _cardArrow.Hide();
        }
    }
}
