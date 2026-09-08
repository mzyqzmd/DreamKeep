using System;
using System.Collections.Generic;

namespace _DreamKeep.Gameplay
{
    /// <summary>结算管线拦截点注册表：按注册顺序同步执行，取代被否决的广播式事件总线。</summary>
    public class HookRegistry
    {
        private readonly Dictionary<Type, List<IActionHook>> _preHooks = new();
        private readonly Dictionary<Type, List<IActionHook>> _postHooks = new();
        private readonly Dictionary<Type, List<IBattleHook>> _pointHooks = new();

        #region 订阅动作级 Pre 钩子
        public void SubscribePre<T>(IActionHook hook_) where T : GameAction => AddActionHook(_preHooks, typeof(T), hook_);
        #endregion

        #region 订阅动作级 Post 钩子
        public void SubscribePost<T>(IActionHook hook_) where T : GameAction => AddActionHook(_postHooks, typeof(T), hook_);
        #endregion

        #region 取消订阅动作级钩子
        public void UnsubscribePre<T>(IActionHook hook_) where T : GameAction => RemoveActionHook(_preHooks, typeof(T), hook_);
        public void UnsubscribePost<T>(IActionHook hook_) where T : GameAction => RemoveActionHook(_postHooks, typeof(T), hook_);
        #endregion

        #region 订阅流程级钩子点
        public void SubscribePoint<TPoint>(IBattleHook hook_) where TPoint : HookPoint
        {
            Type type = typeof(TPoint);
            if (!_pointHooks.ContainsKey(type))
                _pointHooks[type] = new List<IBattleHook>();
            _pointHooks[type].Add(hook_);
        }
        #endregion

        #region 触发动作级 Pre 钩子
        public void InvokePre(GameAction action_) => InvokeActionHooks(_preHooks, action_);
        #endregion

        #region 触发动作级 Post 钩子
        public void InvokePost(GameAction action_) => InvokeActionHooks(_postHooks, action_);
        #endregion

        #region 触发流程级钩子点
        public void InvokePoint<TPoint>(BattleState battleState_) where TPoint : HookPoint
        {
            if (!_pointHooks.TryGetValue(typeof(TPoint), out List<IBattleHook> hooks)) return;
            foreach (IBattleHook hook in new List<IBattleHook>(hooks))
                hook.OnHook(battleState_);
        }
        #endregion

        // 沿继承链向上查找：订阅基类即可收到全部子类动作，快照遍历避免遍历中被修改
        private static void InvokeActionHooks(Dictionary<Type, List<IActionHook>> hooks_, GameAction action_)
        {
            Type type = action_.GetType();
            while (type != null && typeof(GameAction).IsAssignableFrom(type))
            {
                if (hooks_.TryGetValue(type, out List<IActionHook> list))
                    foreach (IActionHook hook in new List<IActionHook>(list))
                        hook.Invoke(action_);
                type = type.BaseType;
            }
        }

        private static void AddActionHook(Dictionary<Type, List<IActionHook>> hooks_, Type type_, IActionHook hook_)
        {
            if (!hooks_.ContainsKey(type_))
                hooks_[type_] = new List<IActionHook>();
            hooks_[type_].Add(hook_);
        }

        private static void RemoveActionHook(Dictionary<Type, List<IActionHook>> hooks_, Type type_, IActionHook hook_)
        {
            if (!hooks_.TryGetValue(type_, out List<IActionHook> list)) return;
            list.Remove(hook_);
            if (list.Count == 0)
                hooks_.Remove(type_);
        }
    }
}
