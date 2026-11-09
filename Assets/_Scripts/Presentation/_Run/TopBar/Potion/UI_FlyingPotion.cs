using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using _DreamKeep.EngineUtility;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.Presentation
{
    /// <summary>飞行药水（纯视图）：把调用方给的药水图标从槽位位置沿弧线自转飞到落点，落地收缩后收起。
    /// 它不认识事件、药水与生物——落点由调用方按玩法层的解析算好（多目标给多个点，这里取几何中心）；
    /// 静默时图标关闭且不占指针射线。</summary>
    public sealed class UI_FlyingPotion : MonoBehaviour
    {
        [Tooltip("图标：不接时退回自身上的 Image")] [SerializeField]
        private Image _icon;

        [Tooltip("本次飞行的总时长（秒）")] [SerializeField]
        private float _duration = 0.35f;

        [Tooltip("弧线高度：以起点到落点的弦长为基准的抬升比例")] [SerializeField]
        private float _arcHeightFactor = 0.25f;

        [Tooltip("飞行途中绕 Z 轴自转的圈数")] [SerializeField]
        private float _spinTurns = 1f;

        [Tooltip("落地后收缩消失的时长（秒）")] [SerializeField]
        private float _vanishDuration = 0.1f;

        private Tween _flightTween;
        private Tween _vanishTween;
        private float _progress;

        private void Awake() => Hide();

        private void OnDestroy()
        {
            _flightTween?.Kill();
            _vanishTween?.Kill();
        }

        #region 飞行：从起点飞到落点集合的几何中心，落地收缩收起

        /// <summary>把 icon_ 摆到 from_ 并飞到落点集合的中心：播完（含收缩）才返回；
        /// 宿主在途中销毁时静默放行，不卡住等待它的战斗链路。</summary>
        public async UniTask PlayAsync(Vector3 from_, IReadOnlyList<Vector3> points_, Sprite icon_)
        {
            if (points_ == null || points_.Count == 0) return;

            Vector3 start = UiGeometry.KeepPlane(from_, transform.position.z);
            Vector3 end = UiGeometry.KeepPlane(CenterOf(points_), transform.position.z);
            Vector3 control = (start + end) * 0.5f + Vector3.up * (Vector3.Distance(start, end) * _arcHeightFactor);

            _icon.sprite = icon_;
            _icon.enabled = true;
            transform.localScale = Vector3.one;
            _progress = 0f;
            ApplyPose(start, control, end, 0f);
            UiTween.PlayScalar(ref _flightTween, () => _progress,
                value_ => ApplyPose(start, control, end, _progress = value_), 1f, _duration);

            if (await CanceledAsync(_duration)) return;

            UiTween.PlayScalar(ref _vanishTween, () => transform.localScale.x,
                value_ => transform.localScale = Vector3.one * value_, 0f, _vanishDuration);
            if (await CanceledAsync(_vanishDuration)) return;

            Hide();
        }

        #endregion

        // 等待一段时间；宿主销毁则整段演出作废。补间只管观感，计时与放行一律走这里，
        // 避免实例被销毁后补间回调继续写已销毁的 transform
        private UniTask<bool> CanceledAsync(float seconds_) =>
            UniTask.Delay(TimeSpan.FromSeconds(seconds_), cancellationToken: destroyCancellationToken)
                .SuppressCancellationThrow();

        // 弧线取点与平面内自转：位置沿二次贝塞尔走，朝向只绕 Z 转，起飞与落点朝向都不参与
        private void ApplyPose(Vector3 start_, Vector3 control_, Vector3 end_, float t_)
        {
            transform.position = Curve.Point(start_, control_, end_, t_);
            transform.localRotation = Quaternion.AngleAxis(_spinTurns * 360f * t_, Vector3.forward);
        }

        // 多目标的落点中心：目标数上限是敌人数，逐点求和即可
        private static Vector3 CenterOf(IReadOnlyList<Vector3> points_)
        {
            Vector3 sum = points_[0];
            for (int i = 1; i < points_.Count; i++) sum += points_[i];
            return sum / points_.Count;
        }

        private void Hide()
        {
            _icon.enabled = false;
            transform.localScale = Vector3.one;
        }
    }
}