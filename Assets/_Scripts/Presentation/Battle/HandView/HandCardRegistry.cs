using System.Collections.Generic;
using _DreamKeep.RuntimeData;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace _DreamKeep.Presentation
{
    /// <summary>手牌本体与交互资格：一半是台账——卡池、手牌列、模型到包装的索引都由它保管，它只管"谁在手牌里"，牌面内容与动效归包装（UI_CardHolder）；
    /// 一张牌从取出到回收的整段生命周期都从这里过：Spawn 一次完成取池、绑卡、接入宿主、进手牌列与标记入场中，离手与归宿则由 RemoveFromHand / Release 收尾。
    /// 另一半是交互状态的唯一持有者兼判据——本回合可打、正被拖拽、正被悬停三个状态位都收在这里；
    /// "已打出、等结算"的成员关系刻意不在这里，由出牌区队列唯一持有（判忙与对账都向它问），两处不再各记一份。
    /// 两条判据——悬停资格（本回合可打、还在手牌里、没有牌正被拖拽、本张没在出牌区排队、没在入场飞行，且位置补间已停）与
    /// 抓取资格（悬停资格之上再要求"整手牌都抽完落稳"，抽牌途中不许把牌拖走）。
    /// 位置补间在飞的牌一般不参与悬停：它的补间会逐帧写回槽位姿态，把悬停姿态顶掉；只有当前悬停中的牌是例外，
    /// 排布对它只走不写层归属的滑移通道，悬停照样跟手牌一起让位。
    /// 层归属也收在这里（MoveToHand / MoveToQueue / MoveToFly / MoveToFocus）：谁此刻属于哪一层由状态决定，
    /// 层内兄弟位按各层语义给（手牌靠右压靠左、队列队首在最上、飞行与交互追加置末），层与层之间的先后则由四个卡层在手牌中心节点下的兄弟顺序静态决定。
    /// 可打状态初值取不可打最保守：任何漏掉的阶段信号都只会让手牌安静，不会让手牌误响应；
    /// 状态切换时要不要清悬停、重排、刷外观由调用方（演出侧）编排。</summary>
    public class HandCardRegistry
    {
        private readonly ObjectPool<UI_CardHolder> _cardPool;
        private readonly UI_HandView _host;
        private readonly UI_PlayCardZone _dropZone;
        private readonly Transform _handLayer;
        private readonly Transform _queueLayer;
        private readonly Transform _flyLayer;
        private readonly Transform _focusLayer;
        private readonly List<UI_CardHolder> _hand = new();

        private readonly Dictionary<CardModel, UI_CardHolder> _views = new();

        private readonly HashSet<UI_CardHolder> _entering = new();

        // 交互状态位：本回合可打、正被拖拽、正被悬停
        private bool _cardsPlayable;
        private UI_CardHolder _draggedCard;
        private UI_CardHolder _hoveredCard;

        public HandCardRegistry(UI_CardHolder cardPrefab_, UI_HandView host_, UI_PlayCardZone dropZone_,
            Transform handLayer_, Transform queueLayer_, Transform flyLayer_, Transform focusLayer_)
        {
            _host = host_;
            _dropZone = dropZone_;
            _handLayer = handLayer_;
            _queueLayer = queueLayer_;
            _flyLayer = flyLayer_;
            _focusLayer = focusLayer_;
            _cardPool = new ObjectPool<UI_CardHolder>(
                () => Object.Instantiate(cardPrefab_, handLayer_),
                view_ => view_.gameObject.SetActive(true),
                view_ =>
                {
                    view_.KillMotion();
                    view_.gameObject.SetActive(false);
                    _entering.Remove(view_); // 回收即注销入场标记与交互身份，防实例复用后残留旧状态
                    if (_hoveredCard == view_) _hoveredCard = null;
                    if (_draggedCard == view_) _draggedCard = null;
                }, defaultCapacity: 10, maxSize: 64); // 洗回时同时借出手牌与整摞飞行牌，池上限要容得下两者之和
        }

        /// <summary>手牌列（按落位顺序）：排布求解与逐帧裁决都按它遍历，只读不直接增删。</summary>
        public IReadOnlyList<UI_CardHolder> Hand => _hand;

        /// <summary>手牌张数：排布求解与"第一张恒居中"的锚定都用它。</summary>
        public int Count => _hand.Count;

        /// <summary>手牌里还有牌在抽牌入场补间中：抓取闸门按它决定"牌全部抽完落稳之前整手不许拖"。</summary>
        public bool HasEntering => _entering.Count > 0;

        /// <summary>本回合手牌是否可打：排布按它决定整手牌要不要下沉，出牌流程按它决定松手要不要打出。</summary>
        public bool CardsPlayable => _cardsPlayable;

        /// <summary>当前悬停中的牌（没有则为 null）：排布按它决定这张牌走不走不写层归属的滑移通道。</summary>
        public UI_CardHolder HoveredCard => _hoveredCard;

        #region 层归属：谁此刻属于哪一层、在层内排第几

        // 手牌层：换层并按手牌列下标插位（靠右压靠左）；不在手牌列里（正在回收的牌）只换层
        public void MoveToHand(UI_CardHolder card_) => card_.MoveToLayer(_handLayer, _hand.IndexOf(card_));

        // 队列层：最新入队的置首，即排在层内最下（出牌区层内"队首在最上"）
        public void MoveToQueue(UI_CardHolder card_) => card_.MoveToLayer(_queueLayer, 0);

        // 飞行层：离场、洗回与遗忘借道一律追加置末
        public void MoveToFly(UI_CardHolder card_) => card_.MoveToLayer(_flyLayer, -1);

        // 交互层：悬停与拖拽一律追加置末，后进来的那张（抓起的）自然压过悬停那张
        public void MoveToFocus(UI_CardHolder card_) => card_.MoveToLayer(_focusLayer, -1);

        #endregion

        // 取出并登记：取池 → 绑卡 → 接入宿主 → 进手牌列 → 归手牌层 → 标记入场中
        public UI_CardHolder Spawn(CardModel model_)
        {
            UI_CardHolder view = _cardPool.Get();
            view.Bind(model_);
            view.SetHost(_host);
            _views[model_] = view;
            _hand.Add(view);
            MoveToHand(view); // 池复用的实例可能残留别的父节点与兄弟位，取出即归位
            _entering.Add(view);
            return view;
        }

        // 离手：只从手牌列除名，模型索引仍保留——打出后还要按模型把它找回来等到达
        public void RemoveFromHand(UI_CardHolder card_) => _hand.Remove(card_);

        // 归宿收尾：注销模型索引并归还卡池（离场补间已由调用方等完，回收回调会清干净补间）
        public void Release(CardModel model_, UI_CardHolder view_)
        {
            _views.Remove(model_);
            _cardPool.Release(view_);
        }

        // 借一张飞行用的包装（洗回搬运）：只取池、不入手牌列与模型索引，牌面沿用上次 Bind 的残留内容，由调用方摆位后立刻归还；
        // 出池即换到飞行层，保证借出去的牌一定画在最上层
        public UI_CardHolder RentFlyView()
        {
            UI_CardHolder view = _cardPool.Get();
            MoveToFly(view);
            return view;
        }

        // 归还飞行用的包装：与归宿收尾同一条回收路径，回收回调会清补间、注销入场标记与交互身份
        public void ReturnFlyView(UI_CardHolder view_) => _cardPool.Release(view_);

        public bool TryGetView(CardModel model_, out UI_CardHolder view_) => _views.TryGetValue(model_, out view_);

        public bool Contains(UI_CardHolder card_) => _hand.Contains(card_);

        public bool Contains(CardModel model_) => _views.ContainsKey(model_);

        public int IndexOf(UI_CardHolder card_) => _hand.IndexOf(card_);

        public bool IsEntering(UI_CardHolder card_) => _entering.Contains(card_);

        // 入场中标记只由抽牌入场演出收起（含中途离场的收尾），期间该牌不响应指针
        public void UnmarkEntering(UI_CardHolder card_) => _entering.Remove(card_);

        // 按当前可打状态刷整手牌外观：不可打时整卡压暗，可打时恢复本色
        public void RefreshPlayableLook(bool playable_)
        {
            foreach (UI_CardHolder card in _hand) card.SetPlayableLook(playable_);
        }

        // 切换可打状态：返回是否真的变了，调用方据此决定要不要跑清悬停、重排、刷外观这一串
        public bool SetPlayable(bool playable_)
        {
            if (_cardsPlayable == playable_) return false;
            _cardsPlayable = playable_;
            return true;
        }

        // 拖拽身份：抓起时写入，松手与中断时清空
        public void SetDragged(UI_CardHolder card_) => _draggedCard = card_;

        public void ClearDragged() => _draggedCard = null;

        // 悬停身份：由悬停裁决每帧写入，退出时置空
        public void SetHovered(UI_CardHolder card_) => _hoveredCard = card_;

        // 排布例外：被拖拽或已入出牌区排队等结算的牌各自有独占通道的补间，排布再动一次会互相 Kill
        public bool IsBusy(UI_CardHolder card_) => card_ == _draggedCard || _dropZone.Contains(card_);

        // 悬停资格：逐帧裁决与悬停退出共用。位置补间在飞的牌不可悬停——它的补间会逐帧写回槽位姿态，把悬停姿态顶掉；
        // 当前悬停中的牌由调用方传 ignoreMotion_ 豁免，让它在重排滑行期间保持悬停
        public bool IsHoverable(UI_CardHolder card_, bool ignoreMotion_ = false) =>
            _cardsPlayable && _draggedCard == null && Contains(card_) &&
            !_dropZone.Contains(card_) && !IsEntering(card_) && (ignoreMotion_ || !card_.IsMoving);

        // 抓取资格：悬停资格之上再要求"牌全部抽完落稳"——还有牌在抽牌入场补间中时，整手牌都不响应拖拽
        public bool IsGrabbable(UI_CardHolder card_) => !HasEntering && IsHoverable(card_);
    }
}
