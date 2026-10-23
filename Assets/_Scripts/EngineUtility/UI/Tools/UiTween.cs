using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace _DreamKeep.EngineUtility
{
    /// <summary>通用补间工具箱（无状态）：一次性脉冲缩放、标量补间通道、位姿通道与"等补间结束"的等待胶水，
    /// 由牌堆、目标箭头、单位视图、手牌包装与悬停组件共用。同通道独占由本类保证（建前先杀旧值）；
    /// 宿主销毁时的兜底 Kill 归宿主自己，本类不碰生命周期。</summary>
    public static class UiTween
    {
        #region 对外暴露的补间

        /// <summary>一次性脉冲：从当前缩放放大到 基准×（1+幅度）再回落到基准，两段都是 OutQuad；基准取向量，非等比缩放同样适用。
        /// 再次调用先杀旧补间——第一段不写起点、从当前缩放续弹，故反复打断只会重置节奏，不会越弹越大。</summary>
        public static Tween PlayPunch(ref Tween slot_, Transform target_, Vector3 baseScale_, float punchScale_,
            float upDuration_, float downDuration_)
        {
            slot_?.Kill();
            slot_ = DOTween.Sequence()
                .Append(target_.DOScale(baseScale_ * (1f + punchScale_), upDuration_).SetEase(Ease.OutQuad))
                .Append(target_.DOScale(baseScale_, downDuration_).SetEase(Ease.OutQuad));
            return slot_;
        }

        /// <summary>标量补间通道：从 read_ 读到的当前值补到 to_（透明度、血条值、缩放等都走这里），写入一律经 write_。
        /// 再次调用先杀旧补间，于是从当前值续走而不是跳变；缓动默认 OutQuad。</summary>
        public static Tween PlayScalar(ref Tween slot_, Func<float> read_,
            Action<float> write_, float to_, float duration_, Ease ease_ = Ease.OutQuad)
        {
            slot_?.Kill();
            // 对外只收 Func/Action（不把 DOTween 的委托类型摊到签名上），故这里转一层——DOTween.To 的 getter 只在启动时读一次起点、setter 每帧写
            slot_ = DOTween.To(() => read_(), value_ => write_(value_), to_, duration_).SetEase(ease_);
            return slot_;
        }

        /// <summary>位姿补间通道：位置与朝向一起从当前值补到给定位姿（world 口径），缓动恒 OutQuad。
        /// 再次调用先杀旧补间，于是从当前姿态续走——拖拽落位、悬停偏移共用同一条实现。</summary>
        public static Tween PlayPose(ref Tween slot_, Transform target_, Vector3 toPosition_, Quaternion toRotation_,
            float duration_)
        {
            slot_?.Kill();
            Vector3 fromPosition = target_.position;
            Quaternion fromRotation = target_.rotation;
            float progress = 0f;
            slot_ = DOTween.To(() => progress, value_ => progress = value_, 1f, duration_)
                .SetEase(Ease.OutQuad)
                .OnUpdate(() => target_.SetPositionAndRotation(
                    Vector3.Lerp(fromPosition, toPosition_, progress),
                    Quaternion.Slerp(fromRotation, toRotation_, progress)));
            return slot_;
        }

        /// <summary>等待补间结束：正常播完与中途被 Kill 都放行，杜绝 await 悬挂；已经结束的补间立即返回。</summary>
        public static UniTask WaitAsync(this Tween tween_)
        {
            if (!tween_.IsActive()) return UniTask.CompletedTask;
            UniTaskCompletionSource source = new();
            tween_.OnComplete(() => source.TrySetResult()).OnKill(() => source.TrySetResult());
            return source.Task;
        }

        #endregion
    }
}
