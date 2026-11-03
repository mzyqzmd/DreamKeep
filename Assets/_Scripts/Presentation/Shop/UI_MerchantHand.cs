using DG.Tweening;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>商人手：平时隐藏，掀地毯时从上方落到场景默认位；悬停商品或删牌服务时平滑移到该格中心，
    /// 离开后先等 _returnDelay 再回默认位，等待期间指到别的格子就直接跟过去（等待作废）。
    /// 手只做位移、不播任何骨骼动画；Pivot 落在手指上，故对齐目标时直接让位置等于目标中心，不再额外加偏移。</summary>
    public sealed class UI_MerchantHand : MonoBehaviour
    {
        [Tooltip("进场时从默认位上方多少画布像素落下")] [SerializeField]
        private float _enterDropPx = 300f;

        [Tooltip("进场下落时长（秒）")] [SerializeField]
        private float _enterDuration = 0.3f;

        [Tooltip("悬停移动时长（秒）")] [SerializeField]
        private float _moveDuration = 0.2f;

        [Tooltip("离开商品后回默认位前的等待（秒）：遮住格子之间的空隙，0 即立刻回位")] [SerializeField]
        private float _returnDelay = 0.25f;

        private Vector3 _defaultPosition;
        private Tween _moveTween;
        private Tween _returnTween;

        private void Awake() => _defaultPosition = transform.position;

        // 失活即停：关地毯后不该留着补间继续写已隐藏节点的位置
        private void OnDisable() => KillTweens();

        #region 对外：进场、指向某格、延迟归位与退场

        /// <summary>掀地毯时调用：从默认位上方落到默认位。</summary>
        public void PlayEnter()
        {
            gameObject.SetActive(true);
            KillTweens();
            transform.position = _defaultPosition + CanvasOffset(_enterDropPx);
            _moveTween = transform.DOMove(_defaultPosition, _enterDuration).SetEase(Ease.OutCubic);
        }

        /// <summary>悬停某格：平滑移到该格中心（手指点对齐）；传空即按离开处理（延迟归位）。</summary>
        public void PointAt(Transform target_)
        {
            if (!target_)
            {
                ResetPoint();
                return;
            }

            if (!gameObject.activeSelf) gameObject.SetActive(true);
            MoveTo(target_.position);
        }

        /// <summary>指针离开所有格子：等 _returnDelay 再回默认位，期间被指到别的格子会取消这次等待。</summary>
        public void ResetPoint()
        {
            KillReturn();
            if (_returnDelay <= 0f)
            {
                MoveTo(_defaultPosition);
                return;
            }

            _returnTween = DOVirtual.DelayedCall(_returnDelay, () =>
            {
                if (gameObject.activeSelf) MoveTo(_defaultPosition);
            });
        }

        /// <summary>关地毯：跟着面板一起向上滑 dropPx_ 画布像素后隐藏；已隐藏时重复调用不再播。</summary>
        public void PlayExit(float dropPx_, float duration_)
        {
            if (!gameObject.activeSelf) return;
            KillTweens();
            Vector3 target = transform.position + CanvasOffset(dropPx_);
            _moveTween = transform.DOMove(target, duration_).SetEase(Ease.InCubic).OnComplete(HideImmediate);
        }

        /// <summary>关地毯或进店前：立刻收起，不播任何过渡。</summary>
        public void HideImmediate()
        {
            KillTweens();
            gameObject.SetActive(false);
        }

        #endregion

        private void MoveTo(Vector3 worldPosition_)
        {
            KillReturn();
            _moveTween?.Kill();
            _moveTween = transform.DOMove(worldPosition_, _moveDuration).SetEase(Ease.OutQuad);
        }

        private void KillReturn()
        {
            _returnTween?.Kill();
            _returnTween = null;
        }

        private void KillTweens()
        {
            KillReturn();
            _moveTween?.Kill();
            _moveTween = null;
        }

        // 画布像素到世界位移：父级（画布）的缩放由 TransformVector 一并带上，换分辨率也不跑偏
        private Vector3 CanvasOffset(float canvasPx_) =>
            transform.parent ? transform.parent.TransformVector(new Vector3(0f, canvasPx_, 0f)) : Vector3.zero;
    }
}
