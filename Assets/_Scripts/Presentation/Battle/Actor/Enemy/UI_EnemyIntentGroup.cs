using _DreamKeep.RuntimeData;
using TMPro;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>敌人意图预告组：把展示意图翻译成头顶图标串，纯视图、不订事件——订阅与按敌人过滤归 UI_Enemy。
    /// 混合意图把两条子意图的预制体并排放（HorizontalLayoutGroup 自动排布），带数值槽的图标写"伤害×次数"等数值。</summary>
    public class UI_EnemyIntentGroup : MonoBehaviour
    {
        [Header("意图预制体")] [SerializeField] private RectTransform _attackIntentPrefab;
        [SerializeField] private RectTransform _blockIntentPrefab;
        [SerializeField] private RectTransform _healIntentPrefab;
        [SerializeField] private RectTransform _moveIntentPrefab;
        [SerializeField] private RectTransform _applyPerkIntentPrefab;

        #region 对外展示：按展示意图整组重建头顶图标

        /// <summary>重建图标组：清空旧图标后按意图类型递归生成，null 即清空。</summary>
        public void Show(EnemyIntent intent_)
        {
            Clear();
            if (intent_ != null) Append(intent_);
        }

        #endregion

        // 递归展开：混合意图并排放两条子意图，其余按类型生成对应图标
        private void Append(EnemyIntent intent_)
        {
            switch (intent_)
            {
                case EI_Mixed mixed:
                    if (mixed.First != null) Append(mixed.First);
                    if (mixed.Second != null) Append(mixed.Second);
                    break;
                case EI_Damage damage:
                    SetAmount(Spawn(_attackIntentPrefab), $"{damage.Amount}×{damage.Times}");
                    break;
                case EI_Block block:
                    SetAmount(Spawn(_blockIntentPrefab), block.Amount.ToString());
                    break;
                case EI_Heal heal:
                    SetAmount(Spawn(_healIntentPrefab), heal.Amount.ToString());
                    break;
                case EI_Move:
                    Spawn(_moveIntentPrefab);
                    break;
                case EI_ApplyPerk:
                    Spawn(_applyPerkIntentPrefab);
                    break;
            }
        }

        private RectTransform Spawn(RectTransform prefab_)
        {
            if (prefab_ == null) return null;
            return Instantiate(prefab_, transform);
        }

        // 写数值：预制体带数字文本就写，纯图标静默跳过
        private void SetAmount(RectTransform icon_, string text_)
        {
            if (icon_ == null) return;
            TextMeshProUGUI amount = icon_.GetComponentInChildren<TextMeshProUGUI>(true);
            if (amount != null) amount.text = text_;
        }

        private void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);
        }
    }
}
