using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using _DreamKeep.EngineUtility;
using _DreamKeep.GameRun;

namespace _DreamKeep.Presentation
{
    /// <summary>事件选项组：订阅选项就位事实按视图模型动态构建选项条（错峰淡入、点选后整列置灰并锁），
    /// 可在末尾追加「继续」按钮承接翻页与收段——点它即提交继续命令，其余下标提交点选命令。
    /// 同一组实例承载事件全部步骤的选项（第 1 页抉择 / 二段式遗物挑选 / 结果页继续）。
    /// 条目经 MonoObjectPool 复用（单池两桶：选项条与继续按钮，新实例由 onCreate 注入弹层工厂）。</summary>
    public sealed class UI_EventOptionGroup : UI_ButtonGroup
    {
        [Tooltip("选项预制体：运行时按视图模型数量取自池")]
        [SerializeField] private UI_EventOptionItem _optionPrefab;

        [Tooltip("继续按钮预制体：追加在最后一个下标处")]
        [SerializeField] private UI_EventProceedItem _proceedPrefab;

        [Tooltip("相邻两件选项的淡入错峰间隔（秒）")]
        [SerializeField] private float _staggerInterval = 0.08f;

        private readonly List<UI_Button> _items = new(); // 选项条与继续按钮同列，顺序即 AddButton 接线序
        private MonoObjectPool<UI_Button> _pool;
        private EventCommandDispatcher _commands;
        private bool _withProceed;
        private int _optionCount;

        [Inject]
        public void Construct(IObjectResolver resolver_, EventEventDispatcher events_, EventCommandDispatcher commands_)
        {
            _commands = commands_;
            // 单池两桶：Release 按 _originOf 自动寻桶；onCreate 只在新实例上注入一次（气泡发起器要弹层工厂），
            // 复用实例依赖早已就位；归还时收掉悬停展示卡，防带病复活（继续按钮是空壳，复位为无害 no-op）
            _pool = new MonoObjectPool<UI_Button>(transform,
                onCreate_: item => resolver_.InjectGameObject(item.gameObject),
                onReturn_: item => { if (item is UI_EventOptionItem option) option.ResetVisual(); });
            events_.Register<EE_OptionsOffered>(offered_ => Show(offered_.Options, offered_.WithProceed));
        }

        protected override IReadOnlyList<UI_Button> GetButtons() => _items;

        /// <summary>点选定案：先整列置灰（含被点中的那条），再提交意图；
        /// 命令会同步续跑域循环并立即重建选项、把旧实例还池，故置灰必须发生在提交之前。</summary>
        protected override void OnChoiceMade(int index_)
        {
            for (int i = 0; i < _items.Count; i++)
                if (_items[i] is UI_EventOptionItem option) option.SetEnabled(false);

            if (_withProceed && index_ == _optionCount) _commands.Proceed();
            else _commands.Choose(index_);
        }

        /// <summary>把上一轮动态实例与订阅一起还池（各步骤切换与收段时调用）。</summary>
        public void Clear()
        {
            ClearButtons();
            for (int i = _items.Count - 1; i >= 0; i--) _pool.Release(_items[i]);
            _items.Clear();
        }

        /// <summary>按视图模型重建选项列表；withProceed_ 为真时在末尾追加「继续」按钮（下标=选项数）。</summary>
        public void Show(IReadOnlyList<EventOptionViewModel> options_, bool withProceed_)
        {
            _withProceed = withProceed_;
            _optionCount = options_.Count;
            gameObject.SetActive(true); // 组初始失活时先激活：Awake 同步补跑，池实例的组件引用才就绪
            Clear();
            for (int i = 0; i < options_.Count; i++)
            {
                EventOptionViewModel vm = options_[i];
                UI_EventOptionItem slot = (UI_EventOptionItem)_pool.Get(_optionPrefab);
                slot.transform.SetSiblingIndex(i); // 池复用实例兄弟序陈旧，钉位=陈列序
                slot.Bind(vm.Title, vm.Subtitle, vm.Enabled, vm.DisplayCard);
                slot.PlayFadeIn(i * _staggerInterval);
                _items.Add(slot);
                AddButton(slot);
            }

            if (withProceed_)
            {
                UI_EventProceedItem proceed = (UI_EventProceedItem)_pool.Get(_proceedPrefab);
                proceed.transform.SetSiblingIndex(options_.Count);
                _items.Add(proceed);
                AddButton(proceed);
            }

            Unlock(); // 清掉上一轮点选锁，使本轮可重新点选
        }
    }
}
