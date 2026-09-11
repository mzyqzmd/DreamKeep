using Spine.Unity;
using _DreamKeep.Utility;

namespace _DreamKeep.Presentation
{
    /// <summary>循环动画状态：进入后循环播放指定动画，退出无副作用。</summary>
    public class AS_Loop : BaseState<Obj_ActorView>
    {
        private readonly AnimationReferenceAsset _anim;

        public AS_Loop(StateMachine<BaseState<Obj_ActorView>> stateMachine_, Obj_ActorView owner_, AnimationReferenceAsset anim_)
            : base(stateMachine_, owner_) => _anim = anim_;

        public override void Enter() => _owner.Play(_anim, true);
    }
}
