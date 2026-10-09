using System.Threading;
using Cysharp.Threading.Tasks;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>单位动画工具：持骨架句柄与四条公约动画名，只回答"怎么播"，不认识生物、事件与面板。
    /// 骨架未接线时就近取；演出等待锚定宿主销毁令牌，宿主没了即静默放行，不阻塞上层事件链。</summary>
    public class UI_ActorAnimTool : MonoBehaviour
    {
        [SerializeField] private SkeletonGraphic _skeleton; // 演出用骨架

        #region 播放一次播一次动画，播完、被截断或宿主销毁后放行；

        public async UniTask PlayOnceAsync(string animationName_)
        {
            if (!_skeleton.IsValid) _skeleton.Initialize(false);
            TrackEntry entry = _skeleton.AnimationState.SetAnimation(0, animationName_, false);
            UniTaskCompletionSource finished = new();
            entry.Complete += _ => finished.TrySetResult();
            entry.End += _ => finished.TrySetResult();
            // 宿主销毁后骨架不再 Update，Complete/End 都不会再来：销毁令牌把等待方静默放行（不抛异常）
            destroyCancellationToken.Register(() => finished.TrySetResult());
            await finished.Task;
        }

        #endregion

        #region 播放循环动画

        public void PlayLoop(string animationName_)
        {
            if (!_skeleton.IsValid) _skeleton.Initialize(false);
            _skeleton.AnimationState.SetAnimation(0, animationName_, true);
        }

        #endregion

        #region 名字校验：Spine 对不存在的名字会静默播空，装配期报出来

        public void VerifyAnim(params string[] names_)
        {
            if (_skeleton == null)
            {
                Debug.LogError($"[{GetType().Name}] 未绑定 SkeletonGraphic，动画演出不可用");
                return;
            }

            if (!_skeleton.IsValid) _skeleton.Initialize(false);
            SkeletonData data = _skeleton.Skeleton.Data;
            if (data == null)
            {
                Debug.LogError($"[{GetType().Name}] 骨架数据未就绪，无法校验动画名");
                return;
            }

            foreach (var animationName in names_)
                if (data.FindAnimation(animationName) == null)
                    Debug.LogError($"[{GetType().Name}] 骨架缺少动画：{animationName}");
        }

        #endregion
    }
}