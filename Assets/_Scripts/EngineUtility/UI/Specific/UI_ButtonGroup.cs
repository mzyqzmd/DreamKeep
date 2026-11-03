using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _DreamKeep.EngineUtility
{
    /// <summary>按钮组的抽象基类：管理一组 UI_Button，点击其中一个即上报其下标并（默认）锁组，
    /// 通过 WaitChoiceAsync 把选中下标交给调用方。具体选项组继承本类，只需提供按钮清单（GetButtons）；
    /// 需要「选择定案后锁外观」的组覆写 OnChoiceMade 即可。</summary>
    public abstract class UI_ButtonGroup : MonoBehaviour
    {
        private readonly List<UI_Button> _buttons = new();
        private readonly List<Action> _handlers = new();
        private UniTaskCompletionSource<int> _choice;
        private bool _locked;

        #region 对外：点选与解锁

        /// <summary>等待玩家点选：返回被点按钮下标。</summary>
        public UniTask<int> WaitChoiceAsync()
        {
            _choice = new UniTaskCompletionSource<int>();
            return _choice.Task;
        }

        /// <summary>解锁，使下一次 Show 能重新点选。</summary>
        public void Unlock() => _locked = false;

        #endregion

        protected virtual void Awake()
        {
            if (_buttons.Count > 0) return; // 组初始失活时 Show 可能先于 Awake 接线过：防重复订阅与错位下标
            _buttons.AddRange(GetButtons());
            for (int i = 0; i < _buttons.Count; i++)
            {
                int index = i;
                Action handler = () => OnButtonClicked(index);
                _handlers.Add(handler);
                _buttons[i].Clicked += handler;
            }
        }

        /// <summary>子类提供本组按钮清单（顺序即下标）。</summary>
        protected abstract IReadOnlyList<UI_Button> GetButtons();

        protected virtual void OnDestroy()
        {
            for (int i = 0; i < _buttons.Count && i < _handlers.Count; i++)
                _buttons[i].Clicked -= _handlers[i];
        }

        #region 动态重建：选项组在运行时按需换一批按钮时使用

        /// <summary>清掉全部按钮的点击订阅与列表，供 Show 前重建。</summary>
        protected void ClearButtons()
        {
            for (int i = 0; i < _buttons.Count && i < _handlers.Count; i++)
                _buttons[i].Clicked -= _handlers[i];
            _buttons.Clear();
            _handlers.Clear();
            _locked = false;
        }

        /// <summary>追加一个按钮并按当前列表长度当作下标接线；下标在接线时定死，后续增删不影响已接线按钮。</summary>
        protected void AddButton(UI_Button button_)
        {
            int index = _buttons.Count;
            Action handler = () => OnButtonClicked(index);
            _handlers.Add(handler);
            _buttons.Add(button_);
            button_.Clicked += handler;
        }

        #endregion

        private void OnButtonClicked(int index_)
        {
            if (_locked) return;
            _locked = true;
            OnChoiceMade(index_); // 先锁外观再交出去：TrySetResult 会同步续跑消费者，可能立即还池重建选项
            _choice?.TrySetResult(index_);
        }

        /// <summary>选择定案后的扩展点（此时已锁组、还没交给等待方）：子类可在这里统一改写选项外观。</summary>
        protected virtual void OnChoiceMade(int index_)
        {
        }
    }
}
