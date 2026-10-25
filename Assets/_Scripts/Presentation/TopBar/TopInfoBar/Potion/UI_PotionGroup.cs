using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.CommonPresentation;
using _DreamKeep.ConfigData;
using _DreamKeep.EngineUtility;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>药水栏：固定三格槽位，按玩家的药水槽数据绑定图标与悬停说明；点击槽位弹出「使用 / 丢弃」菜单。
    /// 单体落点药水点「使用」后进入待选态——本类自己充当瞄准客户端，由 BattleTargetDirector 每帧解析目标并画箭头，
    /// 点敌人确认、右键或落空取消。可点窗口与手牌一致：玩家回合开始开、回合结束阶段关。
    /// 使用药水的投掷与落地演出也归本类编排：离槽事件自带落点，先清槽再让瓶子飞过去，瓶子落地后逐目标泼上水花，
    /// 落地特效不拖住链路——水花开播当帧就放行，效果动作立刻接上。</summary>
    public sealed class UI_PotionGroup : MonoBehaviour, IAimClient
    {
        private const int NoAimSlot = -1;
        private const int NoPopupSlot = -1;

        [Tooltip("指针离开菜单与槽位后自动收起的宽限（秒）：留出从槽位移进菜单的间隙")] [SerializeField]
        private float _popupDismissDelay = 0.2f;

        [Tooltip("投掷飞行视图：未接时只清槽、不演飞行")] [SerializeField]
        private UI_FlyingPotion _flyingPotion;

        [Tooltip("落地特效的尺寸倍率：挂单位视图根，不继承骨架自身缩放")] [SerializeField]
        private float _splashScale = 1f;

        private readonly List<UI_PotionSlot> _slots = new();
        private readonly List<Creature> _preview = new(1); // 预览目标复用表：待选期间逐帧填充，零分配
        private readonly List<Vector3> _landingPoints = new(4); // 飞行落点复用表：每次使用重填，零分配

        private BattleContext _context;
        private BattleCommandDispatcher _commandDispatcher;
        private BattleQueryDispatcher _queryDispatcher;
        private BattleTargetDirector _targetDirector;
        private UI_ActorRoot _actorRoot;
        private VfxFactory _vfxFactory;
        private UI_PotionPopup _popup;

        private int _aimingSlot = NoAimSlot; // 正在待选的药水槽位
        private int _aimSlot = NoAimSlot; // 中间件回写的目标下标
        private int _popupSlot = NoPopupSlot; // 菜单正在为哪个槽位展开
        private bool _registered;
        private bool _arrowShown;
        private bool _interactable;
        private float _outsideSince = -1f; // 指针开始不在"菜单 ∪ 槽位"内的时刻，用于宽限
        private Camera _canvasCamera;

        #region 瞄准客户端事实：由中间件每帧读取

        public bool IsAiming => _registered && _aimingSlot != NoAimSlot;

        /// <summary>药水待选一律走指针指向：只有单体落点药水会进入待选。</summary>
        public bool UsesPointerTargeting => true;

        public bool ArrowShown => _arrowShown;

        /// <summary>箭头起点：发起瞄准的那瓶药水的槽位位置。</summary>
        public Vector3 AimOrigin => IsAiming ? _slots[_aimingSlot].transform.position : default;

        /// <summary>本轮会命中的目标：单体药水只有指针下那名敌人，越界给 null（中间件据此熄灭高亮）。</summary>
        public IReadOnlyList<Creature> PreviewTargets(int slot_)
        {
            _preview.Clear();
            IReadOnlyList<Creature> alive = _queryDispatcher.GetAliveEnemies();
            if (slot_ < 0 || slot_ >= alive.Count) return null;
            _preview.Add(alive[slot_]);
            return _preview;
        }

        /// <summary>接收目标下标：药水只需要槽位号，目标集合由玩法层在结算时按同一份规则再现算。</summary>
        public void ApplyAimResult(int slot_, IReadOnlyList<Creature> targets_) => _aimSlot = slot_;

        #endregion

        #region 装配：收集场景槽位、绑定药水、订阅离槽与回合窗口

        // 画布相机：菜单与槽位的矩形命中都用它（Screen Space Camera 下不能传 null）
        private void Awake() => _canvasCamera = GetComponentInParent<Canvas>().worldCamera;

        [Inject]
        public void Construct(BattleContext context_, BattleCommandDispatcher commandDispatcher_,
            BattleQueryDispatcher queryDispatcher_, BattleTargetDirector targetDirector_, UI_ActorRoot actorRoot_,
            BattleEventDispatcher eventDispatcher_, VfxFactory vfxFactory_)
        {
            _context = context_;
            _commandDispatcher = commandDispatcher_;
            _queryDispatcher = queryDispatcher_;
            _targetDirector = targetDirector_;
            _actorRoot = actorRoot_;
            _vfxFactory = vfxFactory_;

            // 弹层与槽位同在 PotionBar 下，自愈取一次即可；取不到说明节点摆放与预期不符，装配期就报出来
            _popup = transform.parent ? transform.parent.GetComponentInChildren<UI_PotionPopup>(true) : null;
            if (!_popup)
                Debug.LogError($"[{nameof(UI_PotionGroup)}] 没有找到 {nameof(UI_PotionPopup)}：药水的使用 / 丢弃菜单不可用");
            else
                _popup.Hide();

            CollectSlots();
            for (int i = 0; i < _slots.Count; i++) RefreshSlot(i);

            eventDispatcher_.Register<BE_PotionConsumed>(OnPotionConsumedAsync);
            eventDispatcher_.Register<BE_TurnStarted>(OnTurnStarted);
            eventDispatcher_.Register<BE_PlayerTurnEnded>(OnTurnEnded);
        }

        #endregion

        // 每帧：菜单开着时看指针是否已离开；待选时右键取消、左键落在敌人身上即使用，落空取消。其余时刻零开销
        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return;
            if (_popupSlot != NoPopupSlot) TickPopupDismiss(mouse.position.ReadValue());
            if (!IsAiming) return;
            if (mouse.rightButton.wasPressedThisFrame)
            {
                CancelAim();
                return;
            }

            if (!mouse.leftButton.wasPressedThisFrame) return;

            int slot = _aimingSlot;
            int aimSlot = _aimSlot;
            CancelAim();
            if (aimSlot == NoAimSlot) return;
            _commandDispatcher.UsePotion(slot, aimSlot);
        }

        // 指针离开"菜单 ∪ 槽位"超过宽限期就自动收起：宽限用来穿过槽位与菜单之间的空隙，
        // 从槽位直接移到别处也会在宽限后收起
        private void TickPopupDismiss(Vector2 pointer_)
        {
            if (InsidePopup(pointer_))
            {
                _outsideSince = -1f;
                return;
            }

            if (_outsideSince < 0f)
            {
                _outsideSince = Time.unscaledTime;
                return;
            }

            if (Time.unscaledTime - _outsideSince < _popupDismissDelay) return;
            HidePopup();
        }

        // 范围 = 菜单矩形 ∪ 发起它的槽位矩形：弹出那一刻指针还在槽位上，不该算离开
        private bool InsidePopup(Vector2 pointer_)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint((RectTransform)_popup.transform, pointer_, _canvasCamera))
                return true;
            return RectTransformUtility.RectangleContainsScreenPoint((RectTransform)_slots[_popupSlot].transform,
                pointer_, _canvasCamera);
        }

        #region 槽位点击路由：待选中一律取消，其余情况弹出菜单（空槽只收起已有菜单）

        private void OnSlotClicked(int slotIndex_)
        {
            if (IsAiming)
            {
                CancelAim();
                return;
            }

            // 再点同一格收起；空槽没有可做的操作，只把别的格的菜单收掉
            if (_popupSlot == slotIndex_ || PotionAt(slotIndex_) == null)
            {
                HidePopup();
                return;
            }

            ShowPopup(slotIndex_);
        }

        // 弹菜单：先恢复上一格说明，再抑制本格——菜单期间本格不弹悬停说明，收起时由 HidePopup 补弹
        private void ShowPopup(int slotIndex_)
        {
            if (!_popup) return;
            HidePopup();
            _popupSlot = slotIndex_;
            _slots[slotIndex_].SuppressTip(true);
            _popup.Show((RectTransform)_slots[slotIndex_].transform, _interactable,
                () => UsePotion(slotIndex_), () => DiscardPotion(slotIndex_));
        }

        private void HidePopup()
        {
            if (_popupSlot == NoPopupSlot) return;
            int slot = _popupSlot;
            _popupSlot = NoPopupSlot;
            _outsideSince = -1f;
            if (_popup) _popup.Hide();
            if (slot >= 0 && slot < _slots.Count) _slots[slot].SuppressTip(false);
        }

        #endregion

        #region 使用与丢弃：单体落点先进待选，其余直接下发命令

        private void UsePotion(int slotIndex_)
        {
            HidePopup();
            SO_Potion potion = PotionAt(slotIndex_);
            if (potion == null) return;
            if (potion.TargetType == EPotionTargetType.AnyEnemy)
            {
                BeginAim(slotIndex_);
                return;
            }

            _commandDispatcher.UsePotion(slotIndex_, NoAimSlot);
        }

        private void DiscardPotion(int slotIndex_)
        {
            HidePopup();
            _commandDispatcher.DiscardPotion(slotIndex_);
        }

        #endregion

        #region 待选态：向中间件登记为唯一瞄准客户端，箭头从槽位指向指针

        private void BeginAim(int slotIndex_)
        {
            _aimingSlot = slotIndex_;
            _aimSlot = NoAimSlot;
            _arrowShown = true;
            _registered = true;
            _targetDirector.BeginAim(this);
        }

        private void CancelAim()
        {
            _aimingSlot = NoAimSlot;
            _aimSlot = NoAimSlot;
            _arrowShown = false;
            if (!_registered) return;
            _registered = false;
            _targetDirector.EndAim(this);
        }

        #endregion

        #region 事件响应：离槽刷新单格并演投掷飞行、回合窗口开关可点

        // 离槽：先清槽（占位图立刻顶上），再把瓶子扔向玩法层给出的落点，落地后逐目标泼上水花。
        // 飞行要等（瓶子落地的空间事实），水花不等——开播当帧就返回，伤害数字与各类特效立刻接上
        private async UniTask OnPotionConsumedAsync(BE_PotionConsumed consumed_)
        {
            Vector3 origin = consumed_.SlotIndex >= 0 && consumed_.SlotIndex < _slots.Count
                ? _slots[consumed_.SlotIndex].transform.position
                : default;
            RefreshSlot(consumed_.SlotIndex);
            if (consumed_.Targets == null) return;
            if (_flyingPotion && CollectLandingPoints(consumed_.Targets))
                await _flyingPotion.PlayAsync(origin, _landingPoints, consumed_.Potion.Icon);
            PlaySplashes(consumed_.Targets, consumed_.Potion.LiquidColor);
        }

        private void OnTurnStarted(BE_TurnStarted started_)
        {
            _interactable = started_.IsPlayerTurn;
            if (!_interactable) CancelAim();
            if (_popup && _popupSlot != NoPopupSlot) _popup.SetUsable(_interactable);
        }

        private void OnTurnEnded(BE_PlayerTurnEnded _)
        {
            _interactable = false;
            CancelAim();
            HidePopup();
        }

        #endregion

        // 槽位一次性收集与订阅：场景里固定三格、从不归还，订阅与槽位同生共死
        private void CollectSlots()
        {
            _slots.AddRange(GetComponentsInChildren<UI_PotionSlot>(true));
            if (_slots.Count != Player.PotionSlotCount)
                Debug.LogError($"[{nameof(UI_PotionGroup)}] 场景槽位数 {_slots.Count} 与数据槽位数 " +
                               $"{Player.PotionSlotCount} 不一致");

            for (int i = 0; i < _slots.Count; i++)
            {
                _slots[i].SetSlotIndex(i);
                _slots[i].Clicked += OnSlotClicked;
            }
        }

        // 落点：各目标骨架的中心点（与受击特效同一口径），查不到视图的目标跳过；
        // 一个都查不到就返回 false，由调用方放弃这次飞行（效果照样结算）
        private bool CollectLandingPoints(IReadOnlyList<Creature> targets_)
        {
            _landingPoints.Clear();
            for (int i = 0; i < targets_.Count; i++)
            {
                if (!_actorRoot.TryGetView(targets_[i], out UI_Actor view) || !view) continue;
                _landingPoints.Add(UiGeometry.AnchorPoint((RectTransform)view.Skeleton.transform,
                    EUiAnchor.MiddleCenter));
            }

            return _landingPoints.Count > 0;
        }

        // 落地水花：逐目标各一份，落在骨架底边中点（瓶子本身仍飞到几何中心）；
        // 不 await——水花只管观感，拖住链路会让掉血、回血与数字整体晚 0.6 秒
        private void PlaySplashes(IReadOnlyList<Creature> targets_, Color color_)
        {
            for (int i = 0; i < targets_.Count; i++)
            {
                if (!_actorRoot.TryGetView(targets_[i], out UI_Actor view) || !view) continue;
                RectTransform skeleton = (RectTransform)view.Skeleton.transform;
                _vfxFactory.PlayPotionSplash(view.transform,
                    UiGeometry.AnchorPoint(skeleton, EUiAnchor.BottomCenter), _splashScale, color_);
            }
        }

        // 单格刷新：数据是唯一真源，事件只用来触发重读
        private void RefreshSlot(int slotIndex_)
        {
            if (slotIndex_ < 0 || slotIndex_ >= _slots.Count) return;
            SO_Potion potion = PotionAt(slotIndex_);
            if (potion) _slots[slotIndex_].Bind(potion);
            else _slots[slotIndex_].SetEmpty();
            if (_aimingSlot == slotIndex_) CancelAim();
            if (_popupSlot == slotIndex_ && !potion) HidePopup();
        }

        private SO_Potion PotionAt(int slotIndex_)
        {
            IReadOnlyList<SO_Potion> potions = _context.Player.PotionSlots;
            return slotIndex_ >= 0 && slotIndex_ < potions.Count ? potions[slotIndex_] : null;
        }
    }
}
