using System;
using DG.Tweening;
using UnityEngine;

namespace _DreamKeep.EngineUtility
{
    /// <summary>呼吸相位发生器：只推相位、不写缩放，缩放的唯一写者是订阅了 PhaseChanged 的宿主（地图节点上是 UI_HoverScaler）。
    /// 两种触发：自驱模式（默认，挂到任意对象上即生效）激活即自动呼吸；被控模式关掉 _selfDriven，由宿主按需调 SetBreath。
    /// 与悬停互不依赖，单独存在时只是空转的相位、没有视觉。幅度相对当前缩放（0 = 关），周期单位秒，走 unscaled 时间。</summary>
    public sealed class UI_HoverBreath : MonoBehaviour
    {
        [Tooltip("自驱模式：开＝激活即自动呼吸（挂到任意对象上即生效）；关＝由宿主调 SetBreath 按需开关")] [SerializeField]
        private bool _selfDriven = true;

        [Tooltip("呼吸幅度（相对当前缩放的比例，0 = 关闭）")] [SerializeField]
        private float _breathAmplitude = 0.1f;

        [Tooltip("呼吸整周期（秒）")] [SerializeField]
        private float _breathPeriod = 2.4f;

        private Tween _breathTween;
        private float _breath01; // 呼吸相位 0..1

        /// <summary>相位每次变化都会触发：缩放写者据此重写缩放。</summary>
        public event Action PhaseChanged;

        /// <summary>当前呼吸因子：乘进缩放的系数，关闭时恒 1。</summary>
        public float Factor => 1f + _breath01 * _breathAmplitude;

        // 自驱：激活即呼吸；被控：等宿主调 SetBreath
        private void OnEnable()
        {
            if (_selfDriven) SetBreath(true);
        }

        // 失活即收补间归零相位，防池化实例带着呼吸态被复用；相位归零会通知写者把缩放写回基准
        private void OnDisable() => SetBreath(false);

        private void OnDestroy() => _breathTween?.Kill();

        #region 开关：被控模式入口

        /// <summary>开关呼吸：只有需要提示的状态才打开；关闭即收补间并归零相位、通知一次写回基准。</summary>
        public void SetBreath(bool on_)
        {
            if (!on_ || _breathAmplitude <= 0f)
            {
                _breathTween?.Kill();
                _breathTween = null;
                if (_breath01 == 0f) return;
                _breath01 = 0f;
                PhaseChanged?.Invoke();
                return;
            }

            if (_breathTween != null && _breathTween.IsActive()) return;
            _breathTween = DOTween.To(() => 0f, value_ =>
                {
                    _breath01 = value_;
                    PhaseChanged?.Invoke();
                }, 1f, Mathf.Max(0.05f, _breathPeriod * 0.5f))
                .SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true);
        }

        #endregion
    }
}
