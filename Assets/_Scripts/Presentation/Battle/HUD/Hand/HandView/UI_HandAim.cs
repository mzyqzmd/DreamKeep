using System.Collections.Generic;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>手牌瞄准器：与手牌区本体同挂一个节点，手牌作为"瞄准客户端"的全部事实长在这里——
    /// 瞄准身份、箭头是否已展开、瞄准结果的两个同步缓存（目标槽位与目标集合），以及整手牌的数值预览对账。
    /// 箭头本体不住这里：抓起 / 松手 / 取消时向战斗侧唯一的瞄准中间件 BattleTargetDirector 登记与注销，
    /// 由它每帧解析结果并回写（回写即连同刷新）。本类每帧只由宿主手牌区驱动一次（兜底取消 + 预览对账），
    /// 不推导落点、不认识任何单位侧实现。</summary>
    public sealed class UI_HandAim : MonoBehaviour, IAimClient
    {
        private const int NoAimSlot = -1; // 尚未指向任何敌人：松手时据此取消，不打出

        private UI_HandView _handView;
        private BattleQueryDispatcher _queryDispatcher;
        private BattleTargetDirector _targetDirector;

        // 瞄准身份：正在走瞄准模式（本卡目标类型为 AnyEnemy）的那张牌；普通卡抓起来也会登记，但这里留空
        private UI_CardBattle _aimingCard;
        private bool _aimingActive; // 箭头是否已展开（指针越过拖动启动阈值）
        private bool _registered; // 是否已向中间件登记为当前瞄准客户端

        // 瞄准结果的两个同步缓存：由中间件每帧一次性回写（ApplyAimResult）。
        // 本类只读不写、不自行推导——松手当帧必须能同步读到槽位，故不能等中间件的下一次回写；
        // 瞄准卡一按下先清成"未指向"，防极快操作下默认值被当成有效目标打出去
        private int _aimSlot = NoAimSlot;
        private IReadOnlyList<Creature> _previewTargets;

        // 生命周期：本体由 RequireComponent 保证同节点必有
        private void Awake() => _handView = GetComponent<UI_HandView>();

        // 装配：数值预览要经上行门面现算最终值；瞄准本身只向中间件登记
        [Inject]
        public void Construct(BattleQueryDispatcher queryDispatcher_, BattleTargetDirector targetDirector_)
        {
            _queryDispatcher = queryDispatcher_;
            _targetDirector = targetDirector_;
        }

        #region 瞄准客户端事实：由中间件每帧读取

        /// <summary>是否需要解析目标：抓起牌即为真（AnyEnemy 卡与锚点落进出牌区的普通卡两种都算），与箭头是否展开无关。</summary>
        public bool IsAiming => _registered && _handView.AimedCard != null;

        /// <summary>是否走指针指向解析：只有 AnyEnemy 卡让指针去敌人命中框里取目标，其余一律按玩法层的落点规则解析。</summary>
        public bool UsesPointerTargeting => IsAiming && _handView.AimedCard.Card.IsAnyEnemyTarget;

        /// <summary>箭头起点：瞄准中那张牌的世界中心（卡对齐、缩放、旋转时自动跟随）。</summary>
        public Vector3 AimOrigin => IsAiming ? _handView.AimedCard.WorldCenter : default;

        /// <summary>箭头是否已展开：指针越过启动阈值后为真；普通卡不走指针指向，恒为假。</summary>
        public bool ArrowShown => _aimingActive;

        /// <summary>本轮会命中的目标：落点规则只有玩法层一份，这里只把瞄准中那张牌交给只读门面现算。</summary>
        public IReadOnlyList<Creature> PreviewTargets(int slot_) =>
            IsAiming ? _queryDispatcher.PreviewTargets(_handView.AimedCard.Card, slot_) : null;

        /// <summary>接收本轮瞄准结果并立刻消费：写入两个缓存后同帧刷新整手牌的数值预览，不存在只更新一半的中间态。</summary>
        public void ApplyAimResult(int slot_, IReadOnlyList<Creature> targets_)
        {
            _aimSlot = slot_;
            _previewTargets = targets_;
            RefreshPreviews();
        }

        #endregion

        #region 瞄准入口：由宿主手牌区在指针事件与状态切换时转发

        // 抓起：登记瞄准身份（只认 AnyEnemy 卡，其余卡留空给松手分流用）、槽位复位成"未指向"，并向中间件登记
        internal void OnGrabbed(UI_CardBattle card_)
        {
            _aimingCard = card_.Card.IsAnyEnemyTarget ? card_ : null;
            _aimingActive = false;
            _aimSlot = NoAimSlot;
            _previewTargets = null;
            if (_registered) return;
            _registered = true;
            _targetDirector.BeginAim(this);
        }

        // 瞄准拖动：越过启动阈值后由包装逐帧上报，首次上报即让箭头展开；此后位姿与颜色随中间件的回写刷新
        internal void OnDragged(UI_CardBattle card_)
        {
            if (card_ != _aimingCard) return;
            _aimingActive = true;
        }

        // 松手：先取回当帧槽位，再注销登记与瞄准身份；返回"这张牌是否走瞄准模式"，宿主据此决定用槽位还是出牌区判定
        internal bool Release(UI_CardBattle card_, out int aimSlot_)
        {
            aimSlot_ = _aimSlot;
            bool aiming = card_ == _aimingCard;
            EndAim();
            return aiming;
        }

        // 拖拽被强制中断（包装禁用或回收）：收箭头清身份，牌的回位由宿主处置
        internal void Cancel(UI_CardBattle card_) => EndAim();

        // 兜底取消：牌已离手、不再被按住或本回合不再可打时收箭头清身份，瞄准卡顺手瞬移回槽位（不播补间）
        internal void CancelActive()
        {
            UI_CardBattle card = _aimingCard;
            EndAim();
            if (card) _handView.DropAimDrag(card);
        }

        #endregion

        // 每帧（由宿主驱动）：先兜底校正瞄准身份，再对账整手牌数值预览
        internal void Tick()
        {
            if (_aimingCard != null && (_handView.DraggedCard != _aimingCard || !_handView.IsInHand(_aimingCard)))
                CancelActive();

            RefreshPreviews();
        }

        // 收瞄准：清身份与结果缓存并注销登记（中间件据此同帧收箭头，不等下一帧）
        private void EndAim()
        {
            _aimingCard = null;
            _aimingActive = false;
            _aimSlot = NoAimSlot;
            _previewTargets = null;
            if (!_registered) return;
            _registered = false;
            _targetDirector.EndAim(this);
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
    }
}
