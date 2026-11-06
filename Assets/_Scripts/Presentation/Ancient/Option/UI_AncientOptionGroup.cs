using System.Collections.Generic;
using UnityEngine;
using VContainer;
using _DreamKeep.ConfigData;
using _DreamKeep.EngineUtility;
using _DreamKeep.GameRun;

namespace _DreamKeep.Presentation
{
    /// <summary>遗物三选一组：订阅候选就位事实按固定三槽 Bind 并错峰淡入，点选一次即锁、只提交下标命令。
    /// 每件是否可选由域层按拾取代价算好平行带出，置灰项仍占位显示。</summary>
    public sealed class UI_AncientOptionGroup : UI_ButtonGroup
    {
        [SerializeField] private UI_AncientOptionItem[] _slots;

        [Tooltip("相邻两件选项的淡入错峰间隔（秒）")]
        [SerializeField]
        private float _staggerInterval = 0.08f;

        private AncientCommandDispatcher _commands;

        [Inject]
        public void Construct(AncientEventDispatcher events_, AncientCommandDispatcher commands_)
        {
            _commands = commands_;
            events_.Register<AE_ChoicesOffered>(offered_ => Show(offered_.Choices, offered_.Claimable));
            events_.Register<AE_ChoicesHidden>(hidden_ => Hide());
        }

        #region 对外展示：Bind 候选错峰淡入，点选一次即锁

        /// <summary>展示本轮候选与逐件可用性：不足三件按实数出，空槽隐藏；淡入为即发即忘的错峰演出。</summary>
        public void Show(IReadOnlyList<SO_Relic> choices_, IReadOnlyList<bool> claimable_)
        {
            gameObject.SetActive(true); // 激活即同步触发 Awake，保证点击路由就绪
            Unlock(); // 清掉上一轮点选锁，使本轮可重新点选
            int count = Mathf.Min(_slots.Length, choices_.Count);
            for (int i = 0; i < _slots.Length; i++)
            {
                if (i >= count)
                {
                    _slots[i].Hide();
                    continue;
                }

                _slots[i].Bind(choices_[i], claimable_[i]);
                _slots[i].PlayFadeIn(i * _staggerInterval);
            }
        }

        public void Hide() => gameObject.SetActive(false);

        #endregion

        protected override IReadOnlyList<UI_Button> GetButtons() => _slots;

        /// <summary>点选定案：提交下标；命令会同步续跑域循环并立即收起选项。</summary>
        protected override void OnChoiceMade(int index_) => _commands.Choose(index_);
    }
}
