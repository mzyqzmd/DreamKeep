using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity; // InjectGameObject 扩展方法住在这里
using _DreamKeep.ConfigData;
using _DreamKeep.EngineUtility;
using _DreamKeep.GameRun;

namespace _DreamKeep.Presentation
{
    /// <summary>遗物栏：整局常驻——按玩家整局持有的遗物逐件生成槽位（由格栅布局自动排布），
    /// 并把遗物播报按遗物引用分派给对应槽位：生效播一次触发表现、用尽则把槽位置灰。
    /// 遗物在整局内只增不减：装配时铺一遍，之后领到新遗物经 Player.RelicAdded 当场补一格；
    /// 播报器是整局单例（战斗效果与休息处挖掘共用），故一次订阅终身有效，不再随段重绑。</summary>
    public sealed class UI_RelicsGroup : MonoBehaviour
    {
        [SerializeField] private UI_RelicSlot _relicSlotPrefab;

        [Tooltip("遗物信息面板预制体：点击遗物槽位时弹出")] [SerializeField]
        private UI_RelicsInfoPanel _relicInfoPanelPrefab;

        [SerializeField] private RectTransform _relicInfoPanelParent;

        private readonly Dictionary<SO_Relic, UI_RelicSlot> _slots = new();
        private MonoObjectPool<UI_RelicSlot> _relicSlotPool;
        private UI_RelicsInfoPanel _relicInfoPanel; // 单实例：同一时刻只会有一张信息面板，开合由面板自己管
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
        public void Construct(IObjectResolver resolver_, RunState run_, RelicTriggerNotifier notifier_)
        {
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

        private void OnRelicAdded(SO_Relic relic_)
        {
            if (_slots.ContainsKey(relic_)) return;
            CreateSlot(relic_);
            _slots[relic_].PlayTrigger(); // 获得遗物：顶栏新槽位播一次涟漪
        }

        // 槽位由本栏生成且从不归还，订阅与槽位同生共死
        private void CreateSlot(SO_Relic relic_)
        {
            UI_RelicSlot slot = _relicSlotPool.Get(_relicSlotPrefab);
            slot.Bind(relic_);
            slot.Clicked += OnSlotClicked;
            _slots[relic_] = slot;
        }

        #endregion

        #region 遗物信息面板：单实例懒建，内容与开合归面板自己

        // 点击槽位：把该遗物与用尽状态交给信息面板（槽位不反向依赖本栏，见 UI_RelicSlot.Clicked 注释）
        private void OnSlotClicked(SO_Relic relic_, bool exhausted_)
        {
            if (!_relicInfoPanel) _relicInfoPanel = Instantiate(_relicInfoPanelPrefab, _relicInfoPanelParent, false);
            _relicInfoPanel.Show(relic_, exhausted_);
        }

        #endregion

        // 生效：涟漪扩散一次、图标短暂提亮
        private void OnRelicTriggered(SO_Relic relic_)
        {
            if (_slots.TryGetValue(relic_, out UI_RelicSlot slot)) slot.PlayTrigger();
        }

        // 用尽：图标定格为灰，表示本场不再生效
        private void OnRelicExhausted(SO_Relic relic_)
        {
            if (_slots.TryGetValue(relic_, out UI_RelicSlot slot)) slot.SetExhausted(true);
        }

        // 节奏计数：顶栏槽位右下角刷新当前计数；非节奏型不会触发，故其数字恒隐藏
        private void OnRelicCountChanged(SO_Relic relic_, int count_)
        {
            if (_slots.TryGetValue(relic_, out UI_RelicSlot slot)) slot.SetCount(count_);
        }
    }
}
