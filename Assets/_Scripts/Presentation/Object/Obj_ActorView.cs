using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Spine;
using Spine.Unity;
using UnityEngine;
using _DreamKeep.Utility;

namespace _DreamKeep.Presentation
{
    /// <summary>角色视图基类：持有骨骼组件与动画状态机，提供循环、一次性与死亡三类播放能力，玩家与敌人共用。</summary>
    public abstract class Obj_ActorView : MonoBehaviour
    {
        [SerializeField] private SkeletonAnimation _skeleton;
        [SerializeField] protected AnimationReferenceAsset _stand;
        [SerializeField] protected AnimationReferenceAsset _act;
        [SerializeField] protected AnimationReferenceAsset _hurt;
        [SerializeField] protected AnimationReferenceAsset _death;

        private readonly StateMachine<BaseState<Obj_ActorView>> _fsm = new();
        private readonly Dictionary<AnimationReferenceAsset, AS_Loop> _loops = new();
        private readonly Dictionary<(AnimationReferenceAsset, AS_Loop), AS_OneShot> _oneShots = new();

        private AS_Loop _standLoop;
        private AS_Dead _dead;
        private UniTaskCompletionSource _pending;

        #region 播循环：立即切换并持续，不等待
        public void PlayLoop(AnimationReferenceAsset anim_)
        {
            EnsureInitialized();
            CompletePending();
            _fsm.ChangeState(GetLoop(OrStand(anim_)));
        }
        #endregion

        // 播一次：播完自动回落待机，返回可等待的播放句柄
        public UniTask PlayOneShot(AnimationReferenceAsset anim_) => PlayOneShotThenLoop(anim_, _stand);

        #region 播一次后转入指定循环：用于举盾起手接举盾待机
        public UniTask PlayOneShotThenLoop(AnimationReferenceAsset oneShot_, AnimationReferenceAsset loop_)
        {
            EnsureInitialized();
            CompletePending();
            AS_Loop fallback = GetLoop(OrStand(loop_));
            if (!IsPlayable(oneShot_))
            {
                _fsm.ChangeState(fallback);
                return UniTask.CompletedTask;
            }
            // 动画缺失时切换状态会同步兑现句柄，因此先取句柄再切换
            _pending = new UniTaskCompletionSource();
            UniTaskCompletionSource pending = _pending;
            _fsm.ChangeState(GetOneShot(oneShot_, fallback));
            return pending.Task;
        }
        #endregion

        #region 播死亡：播完停在最后一帧，不回落
        public UniTask PlayDeath()
        {
            EnsureInitialized();
            CompletePending();
            if (!IsPlayable(_death)) return UniTask.CompletedTask;
            _pending = new UniTaskCompletionSource();
            UniTaskCompletionSource pending = _pending;
            _fsm.ChangeState(_dead);
            return pending.Task;
        }
        #endregion

        // 播放一段动画，动画缺失时返回空条目
        internal TrackEntry Play(AnimationReferenceAsset anim_, bool loop_)
            => IsPlayable(anim_) ? _skeleton.AnimationState.SetAnimation(0, anim_, loop_) : null;

        // 通知一次性动画播完，兑现等待方
        internal void NotifyCompleted() => CompletePending();

        // 骨骼组件的播放状态在自身初始化完成后才有，因此推迟到首次播放时再装配状态机
        private void EnsureInitialized()
        {
            if (_standLoop != null) return;
            _standLoop = GetLoop(_stand);
            _dead = new AS_Dead(_fsm, this, _death);
            _fsm.Initialize(_standLoop);
        }

        private AS_Loop GetLoop(AnimationReferenceAsset anim_)
        {
            if (_loops.TryGetValue(anim_, out AS_Loop loop)) return loop;
            loop = new AS_Loop(_fsm, this, anim_);
            _loops.Add(anim_, loop);
            return loop;
        }

        private AS_OneShot GetOneShot(AnimationReferenceAsset anim_, AS_Loop fallback_)
        {
            (AnimationReferenceAsset, AS_Loop) key = (anim_, fallback_);
            if (_oneShots.TryGetValue(key, out AS_OneShot oneShot)) return oneShot;
            oneShot = new AS_OneShot(_fsm, this, anim_, fallback_);
            _oneShots.Add(key, oneShot);
            return oneShot;
        }

        // 切走一次性动画时旧句柄可能永不被兑现，这里兜底放行避免播放队列卡死
        private void CompletePending()
        {
            if (_pending == null) return;
            UniTaskCompletionSource pending = _pending;
            _pending = null;
            pending.TrySetResult();
        }

        private AnimationReferenceAsset OrStand(AnimationReferenceAsset anim_) => IsPlayable(anim_) ? anim_ : _stand;

        private static bool IsPlayable(AnimationReferenceAsset anim_) => anim_ != null && anim_.Animation != null;
    }
}
