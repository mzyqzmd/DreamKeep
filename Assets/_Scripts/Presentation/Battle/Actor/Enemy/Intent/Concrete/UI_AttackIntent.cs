using _DreamKeep.RuntimeData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.Presentation.Intent
{
    /// <summary>攻击意图图标：按伤害数值选档位图，单次只写伤害、多次写"伤害 × 次数"。
    /// 数据由 UI_EnemyIntentGroup 生成后经 Bind 写入。</summary>
    public class UI_AttackIntent : MonoBehaviour
    {
        [Header("基础配置")] [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _damageText;

        [Header("伤害图标")] [SerializeField] private Sprite _damage0_5;
        [SerializeField] private Sprite _damage6_10;
        [SerializeField] private Sprite _damage11_20;
        [SerializeField] private Sprite _damage21_30;
        [SerializeField] private Sprite _damageMax;

        #region 对外绑定：按伤害意图写图标档位与伤害文本

        public void Bind(EI_Damage damage_)
        {
            int totalDamage = damage_.Amount * damage_.Times;
            _icon.sprite = totalDamage switch
            {
                <= 5 => _damage0_5,
                <= 10 => _damage6_10,
                <= 20 => _damage11_20,
                <= 30 => _damage21_30,
                _ => _damageMax,
            };
            _damageText.text = damage_.Times > 1 ? $"{damage_.Amount} × {damage_.Times}" : damage_.Amount.ToString();
        }

        #endregion
    }
}