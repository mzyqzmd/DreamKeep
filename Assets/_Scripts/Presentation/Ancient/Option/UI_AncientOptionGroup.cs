using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using _DreamKeep.ConfigData;

namespace _DreamKeep.Presentation
{
    /// <summary>遗物三选一组：固定三槽按序 Bind 候选并错峰淡入，点选一次即锁；下标经 WaitChoiceAsync 交给段入口。</summary>
    public sealed class UI_AncientOptionGroup : MonoBehaviour
    {
        [SerializeField] private UI_AncientOptionItem[] _slots;

        [Tooltip("相邻两件选项的淡入错峰间隔（秒）")]
        [SerializeField]
        private float _staggerInterval = 0.08f;

        private UniTaskCompletionSource<int> _choice;
        private bool _locked;

        private void Awake()
        {
            // 槽位是场景预摆的固定件：按下标接线点击路由，捕获局部变量保住各自下标
            for (int i = 0; i < _slots.Length; i++)
            {
                int index = i;
                _slots[i].Clicked += () =>
                {
                    if (_locked) return;
                    _locked = true;
                    _choice?.TrySetResult(index);
                };
            }
        }

        #region 对外展示：Bind 候选错峰淡入，点选一次即锁

        /// <summary>展示本轮候选：不足三件按实数出，空槽隐藏；淡入为即发即忘的错峰演出。</summary>
        public void Show(IReadOnlyList<SO_Relic> choices_)
        {
            gameObject.SetActive(true); // 激活即同步触发 Awake，保证点击路由就绪
            _locked = false;
            int count = Mathf.Min(_slots.Length, choices_.Count);
            for (int i = 0; i < _slots.Length; i++)
            {
                if (i >= count)
                {
                    _slots[i].Hide();
                    continue;
                }

                _slots[i].Bind(choices_[i]);
                _slots[i].PlayFadeIn(i * _staggerInterval);
            }
        }

        public void Hide() => gameObject.SetActive(false);

        /// <summary>等待玩家点选：返回被点槽位下标；领取失败可再调 Show 重开。</summary>
        public async UniTask<int> WaitChoiceAsync()
        {
            _choice = new UniTaskCompletionSource<int>();
            return await _choice.Task;
        }

        #endregion
    }
}
