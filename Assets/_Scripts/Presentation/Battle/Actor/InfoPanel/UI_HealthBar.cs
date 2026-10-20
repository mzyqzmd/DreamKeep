using _DreamKeep.RuntimeData;
using _DreamKeep.EngineUtility;
using _DreamKeep.Utility;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.Presentation
{
    /// <summary>血条组件：Fill 与血量数字一律瞬时到域真值，其后的滞后条按锚点补间收拢，露出"刚刚掉了多少"的橙色缺口；
    /// 回血不做滞后演出；Slider 满值恒为 1、按占比驱动；只认 UI_Actor 转发来的域真值，自己不订事件不判规则。</summary>
    public sealed class UI_HealthBar : MonoBehaviour
    {
        [Header("血条")] [SerializeField] private Gradient _gradient;
        [SerializeField] private Slider _healthSlider;
        [SerializeField] private Image _fillLag;
        [SerializeField] private TextMeshProUGUI _healthText;

        [Tooltip("滞后条补间时长（秒）：掉血后橙色缺口收拢的速度")] [SerializeField]
        private float _fillLagDuration = 0.5f;

        private Creature _creature;
        private Tween _fillLagTween;

        private void OnDestroy() => _fillLagTween?.Kill();

        #region 初始化

        // 初始化：装配期调用——缓存 Fill、把 Slider 定为占比通道（满值 1），再按域真值直写血量
        public void Initialize(Creature creature_)
        {
            _creature = creature_;
            SetHealth(_creature.CurrentHp);
        }

        #endregion

        #region 设置血量

        // 血量：Slider 与文本一律瞬时到真值；滞后条从它当前位置补间到同一占比——掉血时滞后条落在真值之后，露出橙色缺口
        public void SetHealth(int current_)
        {
            _fillLagTween?.Kill(); // 回血与直写路径也要收掉在跑的滞后补间，否则它会继续往旧目标压
            int current = Mathf.Clamp(current_, 0, _creature.MaxHp);
            float normalized = (float)current / _creature.MaxHp;
            _healthSlider.value = normalized;
            _healthText.text = $"{current}/{_creature.MaxHp}";
            UiTween.PlayScalar(ref _fillLagTween, () => _fillLag.rectTransform.anchorMax.x, WriteFillLag, normalized,
                _fillLagDuration);
        }

        // 滞后条：与 Slider 驱动 Fill 同款的锚点伸缩，只写横向上限；数值相等时两根条的矩形逐像素重合
        private void WriteFillLag(float normalized_) => _fillLag.rectTransform.anchorMax = new Vector2(normalized_, 1f);

        #endregion
    }
}