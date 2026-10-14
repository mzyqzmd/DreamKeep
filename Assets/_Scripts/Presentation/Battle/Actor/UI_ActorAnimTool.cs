using System;
using Cysharp.Threading.Tasks;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>单位动画工具：持骨架句柄与四条公约动画名，只回答"怎么播"，不认识生物、事件与面板。
    /// 骨架未接线时就近取；一次动画播到 playRatio_ 比例就放行等待（事件链不必等它播完），
    /// 剩余部分后台继续播、被后一次播放接棒顶掉；自然播完且期间没被接棒时执行 onFinished_，"播完干什么"由调用方在回调里编排。</summary>
    public class UI_ActorAnimTool : MonoBehaviour
    {
        private const float DefaultPlayRatio = 0.5f; // 一次动画播到该比例即放行等待，剩下的后台播完

        private UI_Actor _actor;
        private int _playTicket; // 播放票号：只认最后一次播放请求的播完回调

        private void Awake() => _actor = GetComponent<UI_Actor>();

        #region 播放一次：播到 playRatio_ 比例即放行等待，自然播完且没被接棒时执行 onFinished_

        public async UniTask PlayOnceAsync(string animationName_, float playRatio_ = DefaultPlayRatio,
            Action onFinished_ = null)
        {
            if (!_actor.Skeleton.IsValid) _actor.Skeleton.Initialize(false);
            int ticket = ++_playTicket; // 票号先于 SetAnimation 自增：替换旧条目会同步补发 End，旧的待命回调据此作废
            TrackEntry entry = _actor.Skeleton.AnimationState.SetAnimation(0, animationName_, false);
            UniTaskCompletionSource released = new(); // 放行源：播够比例 / 播完 / 被顶掉 / 宿主销毁，取最早的一个
            bool finished = false;
            entry.Complete += _ => OnCompleted();
            entry.End += _ => OnEnded();

            // 到点放行：事件链不等动画播完（剩下的由后台播完，或被后一次播放接棒顶掉）
            await UniTask.WhenAny(released.Task,
                UniTask.Delay(TimeSpan.FromSeconds(playRatio_ * entry.Animation.Duration),
                    cancellationToken: destroyCancellationToken).SuppressCancellationThrow());
            return;

            // 自然播完：放行等待，并按票号独占地编排收尾（回待机 / 死亡自毁）
            void OnCompleted()
            {
                released.TrySetResult();
                if (finished || ticket != _playTicket) return;
                finished = true;
                onFinished_?.Invoke();
            }

            // 结束或被新的播放请求顶掉：一律放行等待，免得等待挂死；只有仍是最新那条才编排收尾（被顶掉的票号已作废）
            void OnEnded()
            {
                released.TrySetResult();
                if (finished || ticket != _playTicket) return;
                finished = true;
                onFinished_?.Invoke();
            }
        }

        #endregion

        #region 播放循环动画

        public void PlayLoop(string animationName_)
        {
            if (!_actor.Skeleton.IsValid) _actor.Skeleton.Initialize(false);
            _actor.Skeleton.AnimationState.SetAnimation(0, animationName_, true);
        }

        #endregion

        #region 名字校验：Spine 对不存在的名字会静默播空，装配期报出来

        public void VerifyAnim(params string[] names_)
        {
            if (_actor.Skeleton == null)
            {
                Debug.LogError($"[{GetType().Name}] 未绑定 SkeletonGraphic，动画演出不可用");
                return;
            }

            if (!_actor.Skeleton.IsValid) _actor.Skeleton.Initialize(false);
            SkeletonData data = _actor.Skeleton.Skeleton.Data;
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
