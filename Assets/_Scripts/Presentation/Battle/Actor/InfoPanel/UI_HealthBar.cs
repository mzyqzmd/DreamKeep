using _DreamKeep.RuntimeData;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.Presentation
{
    /// <summary>血条组件：Slider 数值滚动补间（DOTween 单通道）+ 滚动文本 + 按血量占比从色带染 Fill，
    /// 只认 UI_Actor 转发来的域真值，自己不订事件不判规则。</summary>
    public sealed class UI_HealthBar : MonoBehaviour
    {
        [Header("血条")] [SerializeField] private Gradient _gradient;
        [SerializeField] private Slider _healthSlider;
        [SerializeField] private TextMeshProUGUI _healthText;

        [Tooltip("血量补间时长（秒）：受伤与回血的滚动速度")] [SerializeField]
        private float _healthTweenDuration = 0.25f;

        private Creature _creature;
        private Image _fillImage;
        private Tween _healthTween;

        private void OnDestroy() => _healthTween?.Kill();

        #region 初始化

        // 初始化：装配期调用——缓存 Fill、按域真值直写血量，不播补间
        public void Initialize(Creature creature_)
        {
            _creature = creature_;
            _fillImage = _healthSlider.fillRect ? _healthSlider.fillRect.GetComponent<Image>() : null;
            _healthSlider.maxValue = _creature.MaxHp;
            SetHealth(_creature.CurrentHp, false);
        }

        #endregion

        #region 设置血量

        // 血量：animate_ 为 false 直写（开局初值），为 true 且值有变才起补间；补间途中再来一次则 Kill 旧的从当前值续走
        public void SetHealth(int current_, bool animate_)
        {
            int current = Mathf.Clamp(current_, 0, _creature.MaxHp);
            _healthTween?.Kill();
            if (animate_ && !Mathf.Approximately(_healthSlider.value, current))
            {
                _healthTween = DOTween.To(() => _healthSlider.value, WriteHealthVisual, current, _healthTweenDuration)
                    .SetEase(Ease.OutQuad);
                return;
            }

            WriteHealthVisual(current);
        }

        #endregion

        // 血条的可视三件套：Slider 值、滚动文本与按血量占比从色带取的 Fill 颜色，补间与直写共用
        private void WriteHealthVisual(float value_)
        {
            _healthSlider.value = value_;
            _healthText.text = $"{Mathf.RoundToInt(value_)}/{_creature.MaxHp}";
            if (_fillImage) _fillImage.color = _gradient.Evaluate(value_ / _creature.MaxHp);
        }
    }
}
