using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using VContainer;
using _DreamKeep.EngineUtility;
using _DreamKeep.GameRun;

namespace _DreamKeep.Presentation
{
    /// <summary>休息选项组：订阅选项就位与收起事实，按列表动态生成选项条（错峰淡入、点选一次即锁），
    /// 悬停哪槽就在说明文本显示该槽「标题\n描述」，移出隐藏；点选只经命令门面提交下标。</summary>
    public sealed class UI_RestOptionGroup : UI_ButtonGroup
    {
        [Tooltip("选项容器：运行时按列表数量在此下 Instantiate 选项预制体")]
        [SerializeField] private Transform _slotRoot;

        [Tooltip("选项预制体：运行时按数量实例化")]
        [SerializeField] private UI_RestOptionItem _optionPrefab;

        [Tooltip("悬停说明文本：未悬停时隐藏")]
        [SerializeField]
        private TMP_Text _description;

        [Tooltip("选项面板根：收段时整体隐藏（按钮 + 说明文本）")]
        [SerializeField]
        private GameObject _panelRoot;

        [Tooltip("相邻两件选项的淡入错峰间隔（秒）")]
        [SerializeField]
        private float _staggerInterval = 0.08f;

        private readonly List<UI_RestOptionItem> _slots = new();
        private readonly List<Action> _hoveredHandlers = new(); // 与 _slots 对位：闭包订阅、退订要用同一实例
        private readonly List<Action> _unhoveredHandlers = new();
        private readonly List<RestOption> _options = new();
        private int _hoveredIndex = -1;
        private RestCommandDispatcher _commands;

        [Inject]
        public void Construct(RestEventDispatcher events_, RestCommandDispatcher commands_)
        {
            _commands = commands_;
            events_.Register<REE_OptionsOffered>(offered_ => Show(offered_.Options));
            events_.Register<REE_OptionsHidden>(hidden_ => Hide());
        }

        #region 对外展示：选项列表 Bind 后错峰淡入，点选一次即锁

        public void Show(IReadOnlyList<RestOption> options_)
        {
            SetPanelVisible(true); // 先激活面板：Instantiate 进失活父级的实例 Awake 不跑，后面的 SetEnabled 会踩空

            // 清掉上一轮的动态实例与订阅
            ClearButtons();
            for (int i = _slots.Count - 1; i >= 0; i--)
            {
                UI_RestOptionItem slot = _slots[i];
                slot.Hovered -= _hoveredHandlers[i];
                slot.Unhovered -= _unhoveredHandlers[i];
                Destroy(slot.gameObject);
            }
            _slots.Clear();
            _hoveredHandlers.Clear();
            _unhoveredHandlers.Clear();

            _options.Clear();
            _options.AddRange(options_);

            for (int i = 0; i < options_.Count; i++)
            {
                UI_RestOptionItem slot = Instantiate(_optionPrefab, _slotRoot, false);
                int index = i;
                Action hovered = () => OnSlotHovered(index);
                Action unhovered = () => OnSlotUnhovered(index);
                slot.Hovered += hovered;
                slot.Unhovered += unhovered;
                slot.SetIcon(options_[i].Icon);
                slot.SetEnabled(options_[i].Enabled);
                slot.PlayFadeIn(i * _staggerInterval);
                _slots.Add(slot);
                _hoveredHandlers.Add(hovered);
                _unhoveredHandlers.Add(unhovered);
                AddButton(slot);
            }

            Unlock(); // 清掉上一轮点选锁，使本轮可重新点选
            _hoveredIndex = -1;
            SetDescriptionVisible(false);
        }

        /// <summary>收段时隐藏整个选项面板（按钮与文本提示一起关）。</summary>
        public void Hide() => SetPanelVisible(false);

        #endregion

        protected override IReadOnlyList<UI_Button> GetButtons() => _slots;

        /// <summary>点选定案：提交下标；命令会同步续跑域循环并立即收起选项，故不再有后续外观处理。</summary>
        protected override void OnChoiceMade(int index_) => _commands.Choose(index_);

        private void OnSlotHovered(int index_)
        {
            _hoveredIndex = index_;
            if (!_description) return;
            RestOption opt = _options[index_];
            _description.gameObject.SetActive(true);
            _description.text = $"<size=150%><color=yellow>{opt.Title}</color></size><br>{opt.Description}";
        }

        private void OnSlotUnhovered(int index_)
        {
            // 选项切换时 Unity 会先 Exit 旧的再 Enter 新的：只在离开当前悬停槽时隐藏，避免闪一帧
            if (_hoveredIndex != index_) return;
            _hoveredIndex = -1;
            SetDescriptionVisible(false);
        }

        private void SetPanelVisible(bool visible_)
        {
            if (_panelRoot) _panelRoot.SetActive(visible_);
            else gameObject.SetActive(visible_);
        }

        private void SetDescriptionVisible(bool visible_)
        {
            if (_description) _description.gameObject.SetActive(visible_);
        }
    }
}
