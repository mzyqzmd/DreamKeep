using System.Collections.Generic;

namespace _DreamKeep.Presentation
{
    /// <summary>手牌交互闸门：手牌交互状态的唯一持有者兼判据——本回合可打、正被拖拽、挂出等待结算、正被悬停四个状态位都收在这里，
    /// 排布（下沉与例外）、出牌流程、悬停裁决一律向它读写，不再各自记一份。
    /// 两条判据——悬停资格（本回合可打、还在手牌里、没有牌正被拖拽、本张没挂出等待结算、没在入场飞行，且位置补间已停）与
    /// 抓取资格（悬停资格之上再要求"整手牌都抽完落稳"，抽牌途中不许把牌拖走）。
    /// 位置补间在飞的牌一般不参与悬停：它的补间会逐帧写回槽位层序，把悬停层序顶掉；只有当前悬停中的牌是例外，
    /// 排布对它只走不写层序的滑移通道，悬停照样跟手牌一起让位。
    /// 初值取不可打最保守：任何漏掉的阶段信号都只会让手牌安静，不会让手牌误响应；
    /// 闸门只持有状态与判据，切换可打状态时要不要清悬停、重排、刷外观由调用方（演出播放器）编排。</summary>
    public class HandInteractionGate
    {
        private readonly HandCardRegistry _registry;
        private readonly HashSet<Obj_Card> _pendingPlays = new();
        private bool _cardsPlayable;
        private Obj_Card _draggedCard;
        private Obj_Card _hoveredCard;

        public HandInteractionGate(HandCardRegistry registry_) => _registry = registry_;

        /// <summary>本回合手牌是否可打：排布按它决定整手牌要不要下沉，出牌流程按它决定松手要不要打出。</summary>
        public bool CardsPlayable => _cardsPlayable;

        /// <summary>当前悬停中的牌（没有则为 null）：排布按它决定这张牌走不走不写层序的滑移通道。</summary>
        public Obj_Card HoveredCard => _hoveredCard;

        /// <summary>挂出等待结算的牌：回合开始按它逐张退回手牌，遍历期间不得改动本集合。</summary>
        public IReadOnlyCollection<Obj_Card> PendingPlays => _pendingPlays;

        // 切换可打状态：返回是否真的变了，调用方据此决定要不要跑清悬停、重排、刷外观这一串
        public bool SetPlayable(bool playable_)
        {
            if (_cardsPlayable == playable_) return false;
            _cardsPlayable = playable_;
            return true;
        }

        // 拖拽身份：抓起时写入，松手与中断时清空
        public void SetDragged(Obj_Card card_) => _draggedCard = card_;

        public void ClearDragged() => _draggedCard = null;

        // 悬停身份：由悬停裁决每帧写入，退出时置空
        public void SetHovered(Obj_Card card_) => _hoveredCard = card_;

        // 挂出集合：松手落点命中后写入，结算或被拒退回时移除
        public void AddPending(Obj_Card card_) => _pendingPlays.Add(card_);

        public bool RemovePending(Obj_Card card_) => _pendingPlays.Remove(card_);

        public void ClearPending() => _pendingPlays.Clear();

        // 排布例外：被拖拽或挂出等待结算的牌各自有独占通道的补间，排布再动一次会互相 Kill
        public bool IsBusy(Obj_Card card_) => card_ == _draggedCard || _pendingPlays.Contains(card_);

        // 悬停资格：逐帧裁决与悬停退出共用。位置补间在飞的牌不可悬停——它的补间会逐帧写回槽位层序，把悬停层序顶掉；
        // 当前悬停中的牌由调用方传 ignoreMotion_ 豁免，让它在重排滑行期间保持悬停
        public bool IsHoverable(Obj_Card card_, bool ignoreMotion_ = false) =>
            _cardsPlayable && _draggedCard == null && _registry.Contains(card_) &&
            !_pendingPlays.Contains(card_) && !_registry.IsEntering(card_) && (ignoreMotion_ || !card_.IsMoving);

        // 抓取资格：悬停资格之上再要求"牌全部抽完落稳"——还有牌在抽牌入场补间中时，整手牌都不响应拖拽
        public bool IsGrabbable(Obj_Card card_) => !_registry.HasEntering && IsHoverable(card_);
    }
}
