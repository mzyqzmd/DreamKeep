using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.Presentation.Intent
{
    /// <summary>攻击意图图标：按伤害数值选档位图，单次只写伤害、多次写"伤害 × 次数"；
    /// 数值随修正实时刷新（敌人的力量/虚弱、玩家的易伤），不套颜色语义——数字变大对玩家是不利信号，与卡面"绿=变强"方向相反；
    /// 数据由 UI_EnemyIntentGroup 生成后经 Bind 写入，悬停说明由基类统一承接。</summary>
    public sealed class UI_AttackIntent : UI_Intent
    {
        [Header("基础配置")] [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _damageText;

        [Header("伤害图标")] [SerializeField] private Sprite _damage0_5;
        [SerializeField] private Sprite _damage6_10;
        [SerializeField] private Sprite _damage11_20;
        [SerializeField] private Sprite _damage21_40;
        [SerializeField] private Sprite _damageMax;

        // 绑定的伤害意图与上一次写入的展示值：预览对账按意图的 Amount/Times 现算，值没变零写入
        private EI_Damage _damage;
        private int _shownAmount = -1;

        #region 对外绑定：按伤害意图写图标档位与伤害文本

        public void Bind(EI_Damage damage_)
        {
            BindIntent(damage_);
            _damage = damage_;
            _shownAmount = -1;
            WriteDamage(damage_.Amount);
        }

        #endregion

        /// <summary>刷新数值预览（意图组对账调用）：伤害经上行门面（来源=敌人、目标内定玩家）现算，展示值未变零写入。</summary>
        public void RefreshPreview(BattleQueryDispatcher queryDispatcher_, Creature source_)
        {
            int resolved = queryDispatcher_.PreviewIntentDamage(source_, _damage.Amount);
            if (_shownAmount != resolved)
                WriteDamage(resolved);
        }

        // 写入：文本按"修正后单段值 × 次数"（数值着色，次数不参与），档位按修正后总值选图
        private void WriteDamage(int amount_)
        {
            _shownAmount = amount_;
            _damageText.text = _damage.Times > 1 ? $"{amount_} × {_damage.Times}" : amount_.ToString();
            _icon.sprite = (amount_ * _damage.Times) switch
            {
                <= 5 => _damage0_5,
                <= 10 => _damage6_10,
                <= 20 => _damage11_20,
                <= 40 => _damage21_40,
                _ => _damageMax,
            };
        }
    }
}