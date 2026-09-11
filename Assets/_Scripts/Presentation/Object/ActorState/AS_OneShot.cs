using Spine;
using Spine.Unity;
using _DreamKeep.Utility;

namespace _DreamKeep.Presentation
{
    /// <summary>一次性动画状态：播放一次，播完先切回落后的循环状态再兑现等待方。</summary>
    public class AS_OneShot : BaseState<Obj_ActorView>
    {
        private readonly AnimationReferenceAsset _anim;
        private readonly AS_Loop _fallback;
        private TrackEntry _entry;

        public AS_OneShot(StateMachine<BaseState<Obj_ActorView>> stateMachine_, Obj_ActorView owner_,
                          AnimationReferenceAsset anim_, AS_Loop fallback_) : base(stateMachine_, owner_)
        {
            _anim = anim_;
            _fallback = fallback_;
        }

        public override void Enter()
        {
            _entry = _owner.Play(_anim, false);
            if (_entry == null)
            {
                _owner.NotifyCompleted();
                return;
            }
            _entry.Complete += OnCompleted;
        }

        public override void Exit() => ReleaseEntry();

        // 回调发生在骨骼推进过程中，先切换状态再兑现，等待方醒来时画面已回到循环
        private void OnCompleted(TrackEntry entry_)
        {
            ReleaseEntry();
            _fsm.ChangeState(_fallback);
            _owner.NotifyCompleted();
        }

        private void ReleaseEntry()
        {
            if (_entry == null) return;
            _entry.Complete -= OnCompleted;
            _entry = null;
        }
    }
}
