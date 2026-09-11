using Spine;
using Spine.Unity;
using _DreamKeep.Utility;

namespace _DreamKeep.Presentation
{
    /// <summary>死亡动画状态：播放一次并停在最后一帧，播完只兑现等待方，不切换状态。</summary>
    public class AS_Dead : BaseState<Obj_ActorView>
    {
        private readonly AnimationReferenceAsset _anim;
        private TrackEntry _entry;

        public AS_Dead(StateMachine<BaseState<Obj_ActorView>> stateMachine_, Obj_ActorView owner_, AnimationReferenceAsset anim_)
            : base(stateMachine_, owner_) => _anim = anim_;

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

        private void OnCompleted(TrackEntry entry_)
        {
            ReleaseEntry();
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
