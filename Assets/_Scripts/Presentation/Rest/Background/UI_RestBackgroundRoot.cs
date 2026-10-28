using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>休息场景背景根：营火与左右木柴余烬的高亮轮廓做循环呼吸闪烁。</summary>
    public sealed class UI_RestBackgroundRoot : MonoBehaviour
    {
        [Header("高亮轮廓")] [SerializeField] private CanvasGroup _leftLogHighLight;
        [SerializeField] private CanvasGroup _rightLogHighLight;
        [SerializeField] private CanvasGroup _fireHighLight;

        [Header("轮廓闪烁边界")] [SerializeField] private float _minAlpha = 0.3f;
        [SerializeField] private float _maxAlpha = 0.7f;

        [Header("呼吸节奏")] [SerializeField] private float _cycleDuration = 1.2f; // 半周期（min→max），整周期约 2.4s

        private readonly List<Tween> _breaths = new();

        private void Start()
        {
            RegisterBreath(_fireHighLight);
            RegisterBreath(_leftLogHighLight);
            RegisterBreath(_rightLogHighLight);
        }

        private void OnDestroy()
        {
            foreach (Tween breath in _breaths) breath?.Kill();
        }

        // 每个高亮从 _minAlpha 起、yoyo 往返到 _maxAlpha 形成呼吸；
        private void RegisterBreath(CanvasGroup highLight_)
        {
            Tween breath = highLight_.DOFade(_maxAlpha, _cycleDuration)
                .From(_minAlpha)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
            breath.fullPosition = _cycleDuration * 0.5f;
            _breaths.Add(breath);
        }
    }
}