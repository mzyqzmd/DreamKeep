using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity; // InjectGameObject 扩展方法住在这里
using _DreamKeep.CommonPresentation;
using _DreamKeep.ConfigData;
using _DreamKeep.EngineUtility;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Presentation
{
    /// <summary>遗物栏：整局常驻——按玩家整局持有的遗物逐件生成槽位（由格栅布局自动排布），
    /// 并把遗物播报按遗物引用分派给对应槽位：生效播一次触发表现、用尽则把槽位置灰。
    /// 允许同款重复持有：每件各占一格，播报按引用广播到同款全部槽位；
    /// 遗物在整局内只增不减：装配时铺一遍，之后领到新遗物经 Player.RelicAdded 当场补一格；
    /// 播报器是整局单例（战斗效果与休息处挖掘共用），故一次订阅终身有效，不再随段重绑。</summary>
    public sealed class UI_RelicsGroup : MonoBehaviour
    {
        [SerializeField] private UI_RelicSlot _relicSlotPrefab;

        private readonly Dictionary<SO_Relic, List<UI_RelicSlot>> _slots = new();
        private MonoObjectPool<UI_RelicSlot> _relicSlotPool;
        private PopPanelFactory _popFactory;
        private RunState _run;
        private RelicTriggerNotifier _notifier;

        private void OnDestroy()
        {
            if (_run != null) _run.Player.RelicAdded -= OnRelicAdded;
            if (_notifier != null)
            {
                _notifier.Triggered -= OnRelicTriggered;
                _notifier.Exhausted -= OnRelicExhausted;
                _notifier.CountChanged -= OnRelicCountChanged;
            }
        }

        // 建池与生成放在同一次注入里：本组件的 Awake 不保证早于容器构建，池就绪与否由自己保证；
        // onCreate_ 只在新实例上补一次容器注入（槽位的悬停气泡要 PopFactory），复用实例不重复注入
        [Inject]
        public void Construct(IObjectResolver resolver_, RunState run_, RelicTriggerNotifier notifier_,
            PopPanelFactory popFactory_)
        {
            _popFactory = popFactory_;
            _run = run_;
            _relicSlotPool = new MonoObjectPool<UI_RelicSlot>(transform,
                onCreate_: slot_ => resolver_.InjectGameObject(slot_.gameObject));
            foreach (SO_Relic relic in run_.Player.Relics)
                CreateSlot(relic);

            // 遗物只增不减：新遗物进来就补一格，无需等下一场战斗重建
            run_.Player.RelicAdded += OnRelicAdded;

            // 播报器住整局容器，本栏也随整局常驻：一次订阅即可覆盖战斗与跑图段（休息处挖掘也在其中）
            _notifier = notifier_;
            if (_notifier != null)
            {
                _notifier.Triggered += OnRelicTriggered;
                _notifier.Exhausted += OnRelicExhausted;
                _notifier.CountChanged += OnRelicCountChanged;
            }
        }

        #region 遗物新增：领到新遗物当场补一格

        private void OnRelicAdded(SO_Relic relic_) => CreateSlot(relic_).PlayTrigger();

        // 槽位由本栏生成且从不归还，订阅与槽位同生共死；同款遗物各占一格
        private UI_RelicSlot CreateSlot(SO_Relic relic_)
        {
            UI_RelicSlot slot = _relicSlotPool.Get(_relicSlotPrefab);
            slot.Bind(relic_);
            slot.Clicked += OnSlotClicked;
            if (!_slots.TryGetValue(relic_, out List<UI_RelicSlot> slots))
            {
                slots = new List<UI_RelicSlot>();
                _slots[relic_] = slots;
            }

            slots.Add(slot);
            return slot;
        }

        #endregion

        // 点击槽位：把该遗物与用尽状态交给工厂单例信息面板（槽位不反向依赖本栏，见 UI_RelicSlot.Clicked 注释）
        private void OnSlotClicked(SO_Relic relic_, bool exhausted_) =>
            _popFactory.AcquireRelicInfoPanel().Show(relic_, exhausted_);

        // 生效：同款全部槽位各播一次涟漪与短暂提亮
        private void OnRelicTriggered(SO_Relic relic_)
        {
            if (!_slots.TryGetValue(relic_, out List<UI_RelicSlot> slots)) return;
            for (int i = 0; i < slots.Count; i++) slots[i].PlayTrigger();
        }

        // 用尽：同款全部槽位定格为灰，表示本场不再生效
        private void OnRelicExhausted(SO_Relic relic_)
        {
            if (!_slots.TryGetValue(relic_, out List<UI_RelicSlot> slots)) return;
            for (int i = 0; i < slots.Count; i++) slots[i].SetExhausted(true);
        }

        // 节奏计数：同款全部槽位的右下角刷新当前计数；非节奏型不会触发，故其数字恒隐藏
        private void OnRelicCountChanged(SO_Relic relic_, int count_)
        {
            if (!_slots.TryGetValue(relic_, out List<UI_RelicSlot> slots)) return;
            for (int i = 0; i < slots.Count; i++) slots[i].SetCount(count_);
        }
    }
}
