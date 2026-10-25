using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity; // InjectGameObject 扩展方法住在这里
using _DreamKeep.ConfigData;
using _DreamKeep.EngineUtility;
using _DreamKeep.GameBattle;
using _DreamKeep.GameRun;

namespace _DreamKeep.Presentation
{
    /// <summary>遗物栏：按玩家持有的遗物逐件生成槽位（由格栅布局自动排布），并把遗物播报按遗物引用分派给对应槽位——
    /// 生效播一次触发表现、用尽则把槽位置灰；遗物在战斗内不变，槽位内容只在生成时写一次，不做增量刷新。</summary>
    public sealed class UI_RelicsGroup : MonoBehaviour
    {
        [SerializeField] private UI_RelicSlot _relicSlotPrefab;

        [Tooltip("遗物信息面板预制体：点击遗物槽位时弹出")] [SerializeField]
        private UI_RelicsInfoPanel _relicInfoPanelPrefab;

        [SerializeField] private RectTransform _relicInfoPanelParent;

        private readonly Dictionary<SO_Relic, UI_RelicSlot> _slots = new();
        private MonoObjectPool<UI_RelicSlot> _relicSlotPool;
        private UI_RelicsInfoPanel _relicInfoPanel; // 单实例：同一时刻只会有一张信息面板，开合由面板自己管

        // 建池与生成放在同一次注入里：本组件的 Awake 不保证早于容器构建，池就绪与否由自己保证；
        // onCreate_ 只在新实例上补一次容器注入（槽位的悬停气泡要 PopFactory），复用实例不重复注入；
        // 播报器与战斗容器同生共死，订阅不注销
        [Inject]
        public void Construct(IObjectResolver resolver_, BattleContext context_, RelicTriggerNotifier notifier_)
        {
            _relicSlotPool = new MonoObjectPool<UI_RelicSlot>(transform,
                onCreate_: slot_ => resolver_.InjectGameObject(slot_.gameObject));
            notifier_.Triggered += OnRelicTriggered;
            notifier_.Exhausted += OnRelicExhausted;
            foreach (SO_Relic relic in context_.Player.Relics)
            {
                UI_RelicSlot slot = _relicSlotPool.Get(_relicSlotPrefab);
                slot.Bind(relic);
                slot.Clicked += OnSlotClicked; // 槽位由本栏生成且从不归还，订阅与槽位同生共死
                _slots[relic] = slot;
            }
        }

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
    }
}