using System;
using System.Collections.Generic;

namespace _DreamKeep.Utility
{
    /// <summary>通用纯 C# 对象池：按创建回调补新实例，取用优先复用空闲实例；归还先复位再入栈，
    /// 超过上限的归还直接丢弃交 GC。与 MonoObjectPool 成对——那个管组件实例（激活、出生父节点），
    /// 这个只管纯 C# 对象，不认预制体、不做实例归属校验。</summary>
    public sealed class ObjectPool<T> where T : class
    {
        private readonly Stack<T> _idle = new(); // 单一种类实例的天生归属：一个池只对应一套创建回调，无需分桶
        private readonly Func<T> _create;
        private readonly Action<T> _onReturn;
        private readonly int _maxSize;

        /// <summary>建池：create_ 只在空闲栈空时触发；maxSize_ 为缓存上限，超出的归还直接丢弃；
        /// onReturn_ 为消费者特有的归还复位。</summary>
        public ObjectPool(Func<T> create_, int maxSize_ = 32, Action<T> onReturn_ = null)
        {
            _create = create_;
            _maxSize = maxSize_;
            _onReturn = onReturn_;
        }

        #region 取还实例：优先复用空闲实例、归还复位入栈

        /// <summary>取出实例：空闲栈非空则弹栈复用，否则按创建回调新建。</summary>
        public T Get() => _idle.Count > 0 ? _idle.Pop() : _create();

        /// <summary>归还实例：先回调复位，再判上限入栈；超过上限的直接丢弃交 GC。</summary>
        public void Release(T instance_)
        {
            if (instance_ == null) return;
            _onReturn?.Invoke(instance_); // 复位先于入栈：下一次取出拿到的就是干净实例
            if (_idle.Count >= _maxSize) return;
            _idle.Push(instance_);
        }

        #endregion
    }
}
