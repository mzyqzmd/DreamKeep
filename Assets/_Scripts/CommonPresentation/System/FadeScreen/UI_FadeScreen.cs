using System;
using _DreamKeep.EngineUtility;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.Presentation
{
    /// <summary>黑屏过渡幕（Scene_MainMenu 的 Canvas_System 自摆节点，System 层盖过一切 UI）：对外唯一入口 CoverAsync——
    /// 激活后淡入纯黑盖住画面并拦截输入，遮罩期间执行 work_，最后无论成败（finally）淡出揭幕再失活；
    /// 补间读写 Image.color.a（节点无 CanvasGroup），拦输入靠激活节点与 Inspector 已配的 raycastTarget。</summary>
    public sealed class UI_FadeScreen : MonoBehaviour
    {
        [Tooltip("淡入时长（秒）")] [SerializeField]
        private float _fadeInSeconds = 0.3f;

        [Tooltip("淡出时长（秒）")] [SerializeField]
        private float _fadeOutSeconds = 0.3f;

        private Image _image;
        private Tween _fadeTween;

        private void Awake() => _image = GetComponent<Image>();

        private void OnDestroy() => _fadeTween?.Kill();

        #region 对外：盖住 → 干活 → 揭开

        /// <summary>黑屏过渡唯一入口：淡入盖住后执行 work_，无论 work_ 正常完成还是中途抛异常都必然淡出揭幕，异常原样上抛。</summary>
        public async UniTask CoverAsync(Func<UniTask> work_)
        {
            gameObject.SetActive(true);
            try
            {
                SetAlpha(0f); // 先归零再淡入：节点序列化时 alpha=1，直接激活会闪一帧黑
                await UiTween.PlayScalar(ref _fadeTween, () => _image.color.a, SetAlpha, 1f, _fadeInSeconds).WaitAsync();
                await work_();
            }
            finally
            {
                await UiTween.PlayScalar(ref _fadeTween, () => _image.color.a, SetAlpha, 0f, _fadeOutSeconds).WaitAsync();
                gameObject.SetActive(false);
            }
        }

        #endregion

        // 只动 alpha 通道，RGB 恒为纯黑
        private void SetAlpha(float alpha_) => _image.color = new Color(0f, 0f, 0f, alpha_);
    }
}
