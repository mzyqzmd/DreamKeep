using System;
using System.Collections.Generic;

namespace _DreamKeep.Gameplay
{
    /// <summary>结算管线拦截点注册表：动作级与流程级统一为单一字典，按触发对象类型沿继承链同步分发，取代被否决的广播式事件总线。</summary>
    public class HookRegistry
    {
        private readonly Dictionary<Type, List<BattleHook>> _hooks = new();

        #region 订阅任意触发器类型的钩子（动作或流程点）
        public void Subscribe<TTrigger>(BattleHook hook_) where TTrigger : class => Add(_hooks, typeof(TTrigger), hook_);
        #endregion

        #region 取消订阅
        public void Unsubscribe<TTrigger>(BattleHook hook_) where TTrigger : class => Remove(_hooks, typeof(TTrigger), hook_);
        #endregion

        #region 统一触发：按 trigger 运行时类型沿继承链向上查找并分发
        public void Invoke(HookContext ctx_)
        {
            object trigger = ctx_.Action ?? (object)ctx_.Point;
            if (trigger == null) return;
            Type type = trigger.GetType();
            while (type != null && (typeof(BattleAction).IsAssignableFrom(type) || typeof(HookPoint).IsAssignableFrom(type)))
            {
                if (_hooks.TryGetValue(type, out List<BattleHook> list))
                    foreach (BattleHook hook in new List<BattleHook>(list))
                        hook.Invoke(ctx_);
                type = type.BaseType;
            }
        }
        #endregion

        #region 动作级便捷入口：Pre 时机（供 ActionSystem 调用）
        public void InvokeActionPre(BattleAction action_, BattleState state_, ActionSystem system_) => Invoke(new HookContext(state_, system_, action_, null, true, false));
        #endregion

        #region 动作级便捷入口：Post 时机（供 ActionSystem 调用）
        public void InvokeActionPost(BattleAction action_, BattleState state_, ActionSystem system_) => Invoke(new HookContext(state_, system_, action_, null, false, true));
        #endregion

        #region 流程级便捷入口：按 HookPoint 类型触发，签名保持 BattleState 使调用点零改动
        public void InvokePoint<TPoint>(BattleState battleState_) where TPoint : HookPoint, new()
        {
            Invoke(new HookContext(battleState_, null, null, new TPoint(), false, false));
        }
        #endregion

        // 沿继承链向上查找：订阅基类即可收到全部子类（动作或流程点），快照遍历避免遍历中被修改
        private static void Add(Dictionary<Type, List<BattleHook>> hooks_, Type type_, BattleHook hook_)
        {
            if (!hooks_.ContainsKey(type_))
                hooks_[type_] = new List<BattleHook>();
            hooks_[type_].Add(hook_);
        }

        private static void Remove(Dictionary<Type, List<BattleHook>> hooks_, Type type_, BattleHook hook_)
        {
            if (!hooks_.TryGetValue(type_, out List<BattleHook> list)) return;
            list.Remove(hook_);
            if (list.Count == 0)
                hooks_.Remove(type_);
        }
    }
}
