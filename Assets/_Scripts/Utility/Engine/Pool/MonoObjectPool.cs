using System;
using System.Collections.Generic;
using UnityEngine;

namespace _DreamKeep.Utility
{
    /// <summary>通用组件池：按预制体分桶，取用时优先复用空闲实例、无空闲才实例化到指定父节点，归还时失活入桶、
    /// 超过上限直接销毁。实例化落点、显隐开关与在用登记由池统一负责，消费者只把"自己特有的复位"写进回调；
    /// 池不认识任何业务类型，也代管不了兄弟位与父节点切换（那属消费者的展示职责）。</summary>
    public sealed class MonoObjectPool<T> where T : Component
    {
        private readonly Transform _parent;
        private readonly int _maxSize;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onReturn;

        private readonly Dictionary<T, T> _originOf = new(); // 在用实例 → 出生预制体：取出即登记、归还即移除，寻桶与合法性校验共用这张表
        private readonly Dictionary<T, Stack<T>> _idle = new(); // 预制体 → 空闲实例栈（一桶一预制体）

        /// <summary>建池：parent_ 为新建实例的出生父节点；maxSize_ 为每桶缓存上限，超出的归还直接销毁；
        /// onGet_/onReturn_ 为消费者特有的取出刷新与归还复位。</summary>
        public MonoObjectPool(Transform parent_, int maxSize_ = 32, Action<T> onGet_ = null, Action<T> onReturn_ = null)
        {
            _parent = parent_;
            _maxSize = maxSize_;
            _onGet = onGet_;
            _onReturn = onReturn_;
        }

        #region 取还实例：按预制体取出、归还回池

        /// <summary>取出实例：优先弹空闲栈（跳过已被外部销毁的伪空实例），栈空才实例化到出生父节点；
        /// 取出即登记为在用并激活回调刷新，兄弟位与父节点由消费者自己摆。</summary>
        public T Get(T prefab_)
        {
            Stack<T> idle = IdleOf(prefab_);
            T instance = null;
            while (idle.Count > 0)
            {
                instance = idle.Pop();
                if (instance) break; // 宿主销毁带走的伪空实例：直接丢弃，它早已不在用表里
            }

            if (!instance) instance = UnityEngine.Object.Instantiate(prefab_, _parent);
            _originOf[instance] = prefab_; // 新实例与复用实例同一行登记，语义即"当前在用"

            instance.gameObject.SetActive(true);
            _onGet?.Invoke(instance);
            return instance;
        }

        /// <summary>归还实例：先回调复位，再失活出表入桶；桶满的直接销毁。非本池实例与重复归还只报错不动池状态。</summary>
        public void Release(T instance_)
        {
            if (!instance_)
            {
                // 真空无处可还；伪空（在用期间被外部销毁）顺手清掉在用登记（字典键按实例 ID 比较，不触碰已销毁的原生对象）
                if (!ReferenceEquals(instance_, null)) _originOf.Remove(instance_);
                return;
            }

            if (!_originOf.Remove(instance_, out T prefab_))
            {
                Debug.LogError($"MonoObjectPool：归还了非本池实例或已归还的实例 {instance_.name}", instance_);
                return;
            }

            // 归还即出表：重复归还下一次自然查不到
            Stack<T> idle = IdleOf(prefab_);
            _onReturn?.Invoke(instance_); // 复位先于失活：失活会触发组件自己的 OnDisable 兜底，复位动作要在它之前
            if (idle.Count >= _maxSize)
            {
                UnityEngine.Object.Destroy(instance_.gameObject);
                return;
            }

            instance_.gameObject.SetActive(false);
            idle.Push(instance_);
        }

        #endregion

        // 取该预制体的空闲桶：懒建，只在首次用到这个预制体时建，不预热实例
        private Stack<T> IdleOf(T prefab_)
        {
            if (_idle.TryGetValue(prefab_, out Stack<T> idle)) return idle;
            idle = new Stack<T>();
            _idle[prefab_] = idle;
            return idle;
        }
    }
}
