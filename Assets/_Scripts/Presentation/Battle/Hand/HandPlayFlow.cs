using System.Collections.Generic;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>手牌区的流程与演出：前半是出牌流程——管住"抓起 → 松手 → 落点判定 → 收进出牌区等待 → 被拒退回 / 归宿离场"这一条线，
    /// 其中 AnyEnemy 卡走瞄准分支：按下即登记瞄准模式（卡定住、不跟手），拖动越过启动阈值后由它驱动目标箭头（每帧从卡中心指向指针），
    /// 松手时指向敌人即按其打出、落空或未激活则取消回位；
    /// 状态不在这里（拖拽与悬停身份、手牌列都在手牌区本体，"已打出、等结算"的成员关系在出牌区队列），
    /// 它只按本体与落点做取舍，判定通过即直接向命令门面发出打出意图——手牌区唯一向域侧发命令的角色。
    /// 后半是十个领域事件的订阅与全部演出——抽牌入场、洗回搬运、打出等待、批量弃牌整波同飞、消耗/遗忘/单张弃牌的归宿飞行、
    /// 被拒退回、回合开始与战斗结束。
    /// "牌进堆 / 离堆"的计数通知在起飞的与落地的那一帧发出（谁飞谁通知），三个牌堆的显示数与弹跳归它们自己；
    /// 洗回与批量弃牌都是整波同飞、一张飞抵即一张进堆，逐张涨数与逐张弹跳因此与飞行的节奏同源。
    /// 它是手牌区里唯一 await 演出播完的角色（域侧逐事件 await，演出播完才继续下一件事），且只消费事件、不判断规则。
    /// 位置与缩放的补间全交包装（UI_CardHolder），它只决定"什么时候让哪张牌去哪、等多久"，且一律给世界姿态：
    /// 槽位由手牌区本体按点现算，牌堆与出牌区的落点直接取各自节点的世界位置，散开幅度按节点的 画布像素→世界 换算；
    /// 换层也在这里按状态发生：抓起进交互层、离场/洗回/遗忘起飞进飞行层，回手与入队由两个区各自负责。
    /// 命令被域侧拒绝时由退回事件走 RejectAsync，回合切换时由 ReconcilePending 把仍挂在出牌区的牌全部对账回手牌。</summary>
    public class HandPlayFlow
    {
        private readonly HandArea _cards;
        private readonly UI_PlayCardZone _dropZone;
        private readonly BattleCommandDispatcher _commandDispatcher;
        private readonly Rng _rng;
        private readonly UI_DrawPile _drawPile;
        private readonly UI_DiscardPile _discardPile;
        private readonly UI_ExhaustPile _exhaustPile;
        private readonly UI_CardArrow _arrow;
        private readonly float _drawGrowDuration;
        private readonly float _drawGrowOvershoot;
        private readonly float _drawSettleDuration;
        private readonly float _exitDuration;
        private readonly float _reshuffleFlyDuration;
        private readonly float _reshuffleFlyScale;
        private readonly float _reshuffleFlySpreadPx;
        private readonly float _reshuffleFlySpreadMaxPx;
        private readonly float _flyJitterPx;

        // 瞄准状态：哪张牌正走"拖箭头指定目标"的模式（AnyEnemy 卡按下即登记），以及箭头是否已经越过启动阈值显示出来
        private UI_CardHolder _aimingCard;
        private bool _aimingActive;

        public HandPlayFlow(HandArea cards_, UI_PlayCardZone dropZone_, BattleCommandDispatcher commandDispatcher_,Rng rng_,
            UI_DrawPile drawPile_, UI_DiscardPile discardPile_, UI_ExhaustPile exhaustPile_, UI_CardArrow arrow_,
            float drawGrowDuration_, float drawGrowOvershoot_, float drawSettleDuration_, float exitDuration_,
            float reshuffleFlyDuration_, float reshuffleFlyScale_, float reshuffleFlySpreadPx_,
            float reshuffleFlySpreadMaxPx_, float flyJitterPx_)
        {
            _cards = cards_;
            _dropZone = dropZone_;
            _commandDispatcher = commandDispatcher_;
            _rng = rng_;
            _drawPile = drawPile_;
            _discardPile = discardPile_;
            _exhaustPile = exhaustPile_;
            _arrow = arrow_;
            _drawGrowDuration = drawGrowDuration_;
            _drawGrowOvershoot = drawGrowOvershoot_;
            _drawSettleDuration = drawSettleDuration_;
            _exitDuration = exitDuration_;
            _reshuffleFlyDuration = reshuffleFlyDuration_;
            _reshuffleFlyScale = reshuffleFlyScale_;
            _reshuffleFlySpreadPx = reshuffleFlySpreadPx_;
            _reshuffleFlySpreadMaxPx = reshuffleFlySpreadMaxPx_;
            _flyJitterPx = flyJitterPx_;
        }

        // 接线：事件清单集中在这里，各事件的演出就在下面，不用翻第二个文件
        public void Register(BattleEventDispatcher dispatcher_)
        {
            dispatcher_.Register<BE_DiscardReshuffled>(OnDiscardReshuffledAsync);
            dispatcher_.Register<BE_CardDrawn>(OnCardDrawnAsync);
            dispatcher_.Register<BE_CardPlayed>(OnCardPlayedAsync);
            dispatcher_.Register<BE_CardDiscarded>(discarded_ =>
                ExitToPileAsync(discarded_.Card, _discardPile)); // 单张弃牌：打出结算后的卡离场（消耗/遗忘时域侧自动跳过）与将来"弃一张"卡效
            dispatcher_.Register<BE_CardsDiscarded>(OnCardsDiscardedAsync);
            dispatcher_.Register<BE_CardExhausted>(exhausted_ =>
                ExitToPileAsync(exhausted_.Card, _exhaustPile));
            // 遗忘按既定口径飞往出牌区域，此处只播表现形式，永久移除与否由域侧决定；只借道不落堆，故不发进堆通知
            dispatcher_.Register<BE_CardErased>(erased_ =>
                ExitToPileAsync(erased_.Card, null));
            dispatcher_.Register<BE_CardPlayRejected>(OnCardPlayRejectedAsync);
            dispatcher_.Register<BE_TurnStarted>(OnTurnStartedAsync);
            dispatcher_.Register<BE_BattleEnded>(OnBattleEndedAsync);
        }

        #region 出牌受理：抓起、松手、落点判定、瞄准与回合对账

        // 抓起：登记拖拽身份、整张牌换到交互层最上（压过悬停那张）并掐掉在飞补间，随后由包装跟手；
        // AnyEnemy 卡在此登记进瞄准模式——包装据此改成"卡定住、拖动交给宿主"，卡牌姿不再跟手
        public void Grab(UI_CardHolder card_)
        {
            _cards.SetDragged(card_);
            _cards.MoveToFocus(card_);
            card_.KillMotion();
            _aimingCard = card_.Card.IsAnyEnemyTarget ? card_ : null;
            _aimingActive = false;
        }

        /// <summary>这张牌此刻是不是走"拖箭头指定目标"的瞄准模式（按下即成立，与是否越过启动阈值无关）：包装据此决定跟手还是上报。</summary>
        public bool IsAiming(UI_CardHolder card_) => _aimingCard == card_;

        // 瞄准拖动：越过启动阈值后由包装逐帧上报，首次上报即展开箭头；此后位姿与颜色由 Tick 逐帧刷新
        public void OnAimDragged(UI_CardHolder card_, bool onTarget_)
        {
            if (card_ != _aimingCard) return;
            _aimingActive = true;
            _arrow.Show(card_.WorldCenter, _cards.PointerWorldPosition(), onTarget_);
        }

        // 瞄准的逐帧驱动：卡离手或不再被按住时兜底取消；箭头展开期间按"卡中心 → 指针"刷新位姿，
        // 并按当前是否指向合法目标（onTarget_）整支变色
        public void Tick(bool onTarget_)
        {
            if (_aimingCard == null) return;
            if (_cards.Dragged != _aimingCard || !_cards.IsInHand(_aimingCard))
            {
                CancelAim();
                return;
            }

            if (_aimingActive) _arrow.Show(_aimingCard.WorldCenter, _cards.PointerWorldPosition(), onTarget_);
        }

        // 松手：以卡牌的锚点（卡根 pivot＝卡下沿中点）是不是落进出牌区的矩形来判（那块矩形即落点真源），命中即视为选定目标打出，
        // 未命中飞回手牌槽位。松手瞬间本回合已不可打（回合权已交出）、或这张牌已在拖拽途中离手（被弃置 / 回收）时也只回位，
        // 不把过期命令塞进队列等下一个玩家回合结算；瞄准模式的牌不看落点矩形，走 ReleaseAim
        public void Release(UI_CardHolder card_, int aimSlot_)
        {
            if (card_ == _aimingCard)
            {
                ReleaseAim(card_, aimSlot_);
                return;
            }

            _cards.ClearDragged();
            if (!_cards.CardsPlayable || !_cards.IsInHand(card_) || !_dropZone.Accepts(card_.WorldPosition))
            {
                _cards.ReturnToSlotAsync(card_).Forget();
                return;
            }

            // 收下即挂出：待结算的成员关系由出牌区队列唯一持有，闸门判忙与回合对账都向它查；换层与落位都在收下那一刻发生
            _dropZone.Enqueue(card_);
            // 入队即让位：这张从手牌行退出、整手立刻合拢，不等域侧回声；域侧确认到达时（OnCardPlayedAsync）已无视觉动作
            _cards.ApplyLayout();
            _commandDispatcher.PlayCard(card_.Card, aimSlot_);
        }

        // 拖拽被强制中断（包装禁用或回收）时直接落回槽位，不播补间；瞄准中的牌走 CancelAim 收箭头
        public void CancelDrag(UI_CardHolder card_)
        {
            if (card_ == _aimingCard)
            {
                CancelAim();
                return;
            }

            _cards.ClearDragged();
            _cards.PlaceToSlot(card_);
        }

        // 回合开始对账：把仍挂在出牌区等结算的牌逐张退回手牌；先拷一份队列快照再退，全部出队后整手重排一次——
        // 行重新开缝，退回的牌与其余牌一起归位；已离手的牌不在行里，由各自的归宿流程收尾
        public void ReconcilePending()
        {
            IReadOnlyList<UI_CardHolder> queued = _dropZone.Queue;
            if (queued.Count == 0) return;

            UI_CardHolder[] snapshot = new UI_CardHolder[queued.Count];
            for (int i = 0; i < snapshot.Length; i++) snapshot[i] = queued[i];

            foreach (UI_CardHolder card in snapshot) _dropZone.Remove(card);
            _cards.ApplyLayout();
        }

        #endregion

        // 瞄准松手：指向了某个敌人（aimSlot 不小于 0）才打出，落空、从未展开箭头、不可打或已离手一律取消回位
        private void ReleaseAim(UI_CardHolder card_, int aimSlot_)
        {
            bool shown = _aimingActive;
            _aimingCard = null;
            _aimingActive = false;
            _cards.ClearDragged();
            if (shown) _arrow.Hide();

            if (!_cards.CardsPlayable || !_cards.IsInHand(card_) || aimSlot_ < 0)
            {
                _cards.ReturnToSlotAsync(card_).Forget();
                return;
            }

            _dropZone.Enqueue(card_);
            _cards.ApplyLayout();
            _commandDispatcher.PlayCard(card_.Card, aimSlot_);
        }

        // 取消瞄准：收箭头、清身份、把牌瞬移回槽位（中断与兜底路径，不播补间）
        private void CancelAim()
        {
            if (_aimingCard == null) return;
            UI_CardHolder card = _aimingCard;
            _aimingCard = null;
            _aimingActive = false;
            _arrow.Hide();
            _cards.ClearDragged();
            _cards.PlaceToSlot(card);
        }

        // 洗回搬运：域侧一次搬完整摞，这里把它演成整波同飞——洗回多少张就飞多少张，一张飞抵即一张进堆；
        // 域侧逐事件 await，这段播完才轮到本次抽牌入场
        private UniTask OnDiscardReshuffledAsync(BE_DiscardReshuffled reshuffled_) => FlyCardsAsync(reshuffled_.Count);

        // 飞行：借同等张数的包装（只取池、不入手牌列与模型索引）从弃牌堆同帧全部飞到抽牌堆并缩到 0，全部到达才算飞完，飞完随即归还卡池；
        // 起落点按对称铺开骨架错开保底不重叠、再叠随机抖动打散轨迹；起飞那一刻通知弃牌堆"牌离堆"（数字随之减），
        // 落地那一刻通知抽牌堆"牌进堆"（数字随之加并弹一下）——两堆的数字与弹跳都由这两条通知推动，这里只管飞；
        // 借来的包装还带着上次 Bind 的残留内容，但整段飞行都缩得很小，不为它单独刷外观
        private async UniTask FlyCardsAsync(int count_)
        {
            int flyCount = count_;
            UI_CardHolder[] cards = new UI_CardHolder[flyCount];
            Vector3 from = _discardPile.transform.position;
            Vector3 to = _drawPile.transform.position;

            // 起飞：逐张摆在弃牌堆附近的散开点上，缩到飞行用的起始尺寸，同帧全部起飞
            for (int i = 0; i < flyCount; i++)
            {
                UI_CardHolder card = _cards.RentFlyView();
                cards[i] = card;
                card.Place(from + FlyOffset(i, flyCount) + Jitter(_discardPile.transform),
                    _discardPile.transform.rotation);
                card.transform.localScale = Vector3.one * _reshuffleFlyScale;
            }

            UniTask[] flights = new UniTask[flyCount];
            for (int i = 0; i < flyCount; i++)
            {
                UI_CardHolder card = cards[i];
                _discardPile.OnCardLeft();
                flights[i] = FlyOneAsync(card, to + FlyOffset(i, flyCount) + Jitter(_drawPile.transform));
            }

            await UniTask.WhenAll(flights);
            for (int i = 0; i < flyCount; i++) _cards.ReturnFlyView(cards[i]);
        }

        // 单张飞行：飞到终点并缩到 0，落定即通知抽牌堆进了一张
        private async UniTask FlyOneAsync(UI_CardHolder card_, Vector3 destination_)
        {
            await UniTask.WhenAll(
                card_.MoveToAsync(destination_, _drawPile.transform.rotation, _reshuffleFlyDuration),
                card_.ScaleToAsync(0f, _reshuffleFlyDuration));
            _drawPile.OnCardArrived();
        }

        // 飞行牌的起落点错开量：横向按序号对称铺开、纵向奇偶反向；整串牌的总跨度不超过上限，张数越多每张越挤，
        // 一摞厚弃牌堆也不会把牌流铺到画布之外。幅度按画布像素给，再按抽牌堆节点的 画布像素→世界 换算成世界向量
        private Vector3 FlyOffset(int index_, int count_)
        {
            float spread = count_ <= 1 ? 0f : Mathf.Min(_reshuffleFlySpreadPx, _reshuffleFlySpreadMaxPx / (count_ - 1));
            float lateral = (index_ - (count_ - 1) * 0.5f) * spread;
            float vertical = (index_ % 2 == 0 ? 1f : -1f) * spread * 0.5f;
            return _drawPile.transform.TransformVector(new Vector2(lateral, vertical));
        }

        // 飞行起落点的随机抖动：±幅度内均匀分布的画布像素，经节点换算成世界向量，只打散重叠轨迹不改朝向
        private Vector3 Jitter(Transform node_) =>
            node_.TransformVector(new Vector2(_rng.Next(-2f,2f), _rng.Next(-2f,2f)) * _flyJitterPx);

        // 抽牌入场：取出登记 → 按当前可打状态刷一次外观 → 零尺寸置于抽牌堆 → 新牌起飞即让老牌让位 → 放大滑翔 → 落位后收正尺寸。
        // 槽位与峰值都按手牌区本体现算的世界姿态与槽位缩放：手牌超过不缩小的张数上限后每张都有缩放，
        // 新牌不能按常态 1 落位，否则比同手牌大一截、命中面也同步变大
        private async UniTask OnCardDrawnAsync(BE_CardDrawn drawn_)
        {
            _drawPile.OnCardLeft(); // 牌从抽牌堆起飞去手牌：先记离堆，数字在入场飞行之前就减
            CardModel model = drawn_.Card;
            UI_CardHolder view = _cards.Spawn(model);
            view.SetPlayableLook(_cards.CardsPlayable); // 将来敌方回合塞牌也不会漏刷
            _cards.EvaluateSlot(_cards.Count - 1, _cards.Count, out Vector3 position, out Quaternion rotation,
                out float slotScale);
            view.Place(_drawPile.transform.position, _drawPile.transform.rotation);
            view.transform.localScale = Vector3.zero;

            // TODO 发牌音效：将来由 Presentation/Feedback 订阅 BE_CardDrawn 播放，不在此处直接调用

            // 新牌起飞的同时就让既有手牌让位张开，不必等新牌落位后再集体挪一次（新牌入场中，重排会跳过它）
            _cards.ApplyLayout();
            await UniTask.WhenAll(
                view.ScaleToAsync(slotScale * _drawGrowOvershoot, _drawGrowDuration),
                view.MoveToAsync(position, rotation, _drawGrowDuration));

            // 入场途中卡已离场（被回收进池）则收尾不再补，避免写到已复用的卡面
            if (!_cards.TryGetView(model, out UI_CardHolder bound) || bound != view)
            {
                _cards.UnmarkEntering(view);
                return;
            }

            await view.ScaleToAsync(slotScale, _drawSettleDuration);
            _cards.UnmarkEntering(view);
            _cards.ApplyLayout(); // 入场期间行若有过变动（别的牌打出 / 让位），在这里一次性纠正到最新槽位与命中面
        }

        // 轮到执行：域侧确认这张已离手（手牌成员关系到此注销），行早在松手那一刻就让过位，这里只等入队补间播完，结算期间不飞不缩
        private async UniTask OnCardPlayedAsync(BE_CardPlayed played_)
        {
            if (!_cards.TryGetView(played_.Card, out UI_CardHolder view)) return;
            _cards.RemoveFromHand(view);
            _cards.ApplyLayout();

            await _dropZone.WaitArrivalAsync(view);
        }

        // 被拒退回：域侧已判明命令不生效，撤下出牌区并整手重排一次——行重新开缝，这张从队列位置飞回自己的槽位；
        // 卡已不在索引里说明已另行处置，不重复动它
        private UniTask OnCardPlayRejectedAsync(BE_CardPlayRejected rejected_)
        {
            if (!_cards.TryGetView(rejected_.Card, out UI_CardHolder view)) return UniTask.CompletedTask;
            if (!_dropZone.Remove(view)) return UniTask.CompletedTask;
            _cards.ApplyLayout();
            return UniTask.CompletedTask;
        }

        // 回合开始：先落可打状态（据此清悬停、重排、刷外观），再把仍挂着等结算的出牌请求对账退回手牌，
        // 最后把三堆数字对齐域侧活值兜底：万有一条进堆 / 离堆通知没发到，最迟在这一刻纠正
        private UniTask OnTurnStartedAsync(BE_TurnStarted started_)
        {
            ApplyPlayable(started_.IsPlayerTurn);
            ReconcilePending();
            _drawPile.SyncToLive();
            _discardPile.SyncToLive();
            _exhaustPile.SyncToLive();
            return UniTask.CompletedTask;
        }

        // 战斗结束：不再有回合交替，手牌一律收起为不可打
        private UniTask OnBattleEndedAsync(BE_BattleEnded ended_)
        {
            ApplyPlayable(false);
            return UniTask.CompletedTask;
        }

        // 可打状态切换：先取消瞄准并让出悬停身份（不可打后就不可交互，ExitHover 只会清状态、会留下半截放大），
        // 再按槽位真源整体重排（位置、缩放与所在层一起回位），最后统一刷整手牌外观；状态没变则什么都不动
        private void ApplyPlayable(bool playable_)
        {
            if (!_cards.SetPlayable(playable_)) return;
            if (!playable_)
            {
                CancelAim();
                _cards.ClearHover();
            }

            _cards.ApplyLayout();
            _cards.RefreshPlayableLook(playable_);
        }

        // 批量弃牌波：整手未保留的牌同帧各自飞向弃牌堆并缩到 0，全部落地才算波次播完（其后域侧才推进到状态衰减与敌方回合）；
        // 起飞逐张记离堆（数字一次掉到底）、落地逐张记进堆（数字随落地递增）；起落位各带随机抖动打散重叠轨迹
        private async UniTask OnCardsDiscardedAsync(BE_CardsDiscarded discarded_)
        {
            Transform target = _discardPile.transform;
            List<UniTask> flights = new();

            // 起飞：逐张出列、先挪到槽位附近散开的随机点再换到飞行层，整手随即合拢
            foreach (CardModel model in discarded_.Cards)
            {
                if (!_cards.TryGetView(model, out UI_CardHolder view)) continue;
                _cards.RemoveFromHand(view);
                _dropZone.Remove(view);
                view.Place(view.WorldPosition + Jitter(target), view.WorldRotation);
                _cards.MoveToFly(view); // 起飞即换到飞行层：飞行的牌一律画在手牌与队列之上
                _discardPile.OnCardLeft();
                flights.Add(FlyToPileAsync(view, model, target));
            }

            if (flights.Count == 0) return;
            _cards.ApplyLayout();

            await UniTask.WhenAll(flights);
        }

        // 波内单张归宿飞行：飞向牌堆并缩到 0，落定即通知进堆、注销索引并回收进池
        private async UniTask FlyToPileAsync(UI_CardHolder view_, CardModel model_, Transform target_)
        {
            await UniTask.WhenAll(
                view_.MoveToAsync(target_.position + Jitter(target_), target_.rotation, _exitDuration),
                view_.ScaleToAsync(0f, _exitDuration));
            _discardPile.OnCardArrived();
            _cards.Release(model_, view_);
        }

        // 归宿：离手 → 出队 → 飞向目标牌堆（传 null 即飞往出牌区）并缩到 0 → 落地通知目标堆进了一张 → 注销索引、回收进池。
        // 进堆通知只在落地这一帧发出，故牌堆的数字与弹跳与飞行同步；目标为 null（遗忘借道）时没有"进堆"这回事，不发通知
        private async UniTask ExitToPileAsync(CardModel model_, UI_Pile arrival_)
        {
            if (!_cards.TryGetView(model_, out UI_CardHolder view)) return;
            _cards.RemoveFromHand(view);
            _dropZone.Remove(view);
            _cards.ApplyLayout();

            Transform target = arrival_ != null ? arrival_.transform : _dropZone.transform;
            _cards.MoveToFly(view); // 起飞即换到飞行层：飞行的牌一律画在手牌与队列之上
            await UniTask.WhenAll(
                view.MoveToAsync(target.position, target.rotation, _exitDuration),
                view.ScaleToAsync(0f, _exitDuration));

            if (arrival_ != null) arrival_.OnCardArrived();
            _cards.Release(model_, view);
        }
    }
}
