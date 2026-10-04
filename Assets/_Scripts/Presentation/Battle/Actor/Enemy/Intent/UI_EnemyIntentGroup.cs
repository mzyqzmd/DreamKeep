using System;
using _DreamKeep.Presentation.Intent;
using _DreamKeep.RuntimeData;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>敌人意图预告组：把展示意图翻译成头顶图标串并分发视觉绑定，负责生成与清空；
    /// 悬停说明由每个图标自己承接（悬停哪一张弹哪一张），组只把气泡宿主交给新生成的图标；
    /// 刷新归持有它的敌人视图 UI_Enemy，自己不订阅域事件。</summary>
    public class UI_EnemyIntentGroup : MonoBehaviour
    {
        [Header("意图预制体")] [SerializeField] private RectTransform _attackIntentPrefab;
        [SerializeField] private RectTransform _blockIntentPrefab;
        [SerializeField] private RectTransform _healIntentPrefab;
        [SerializeField] private RectTransform _moveIntentPrefab;
        [SerializeField] private RectTransform _applyPerkIntentPrefab;

        private UI_PopTextRoot _popTextRoot;

        private UI_PopTextRoot PopTextRoot
        {
            get
            {
                if (_popTextRoot) return _popTextRoot;
                _popTextRoot = FindFirstObjectByType<UI_PopTextRoot>();
                return _popTextRoot ? _popTextRoot : throw new Exception("UI_EnemyIntentGroup 找不到 PopTextRoot 实例");
            }
        }

        #region 对外展示：按展示意图整组重建头顶图标

        /// <summary>重建图标组：清空旧图标后按意图类型递归生成并分发绑定，null 即清空；
        /// 悬停中重建会销毁旧图标（OnDisable 收起气泡），指针下一帧重新进入新图标再弹，最多一帧闪烁。</summary>
        public void Show(EnemyIntent intent_)
        {
            Clear();
            if (intent_ != null) Append(intent_);
        }

        #endregion

        // 递归展开：复合意图并排放两条子意图，其余按类型生成图标、交给对应脚本绑定
        private void Append(EnemyIntent intent_)
        {
            switch (intent_)
            {
                case EI_Mixed mixed:
                    if (mixed.First != null) Append(mixed.First);
                    if (mixed.Second != null) Append(mixed.Second);
                    break;
                case EI_Damage damage:
                    Spawn(_attackIntentPrefab)?.GetComponent<UI_AttackIntent>().Bind(damage);
                    break;
                case EI_Block block:
                    Spawn(_blockIntentPrefab)?.GetComponent<UI_BlockIntent>().Bind(block);
                    break;
                case EI_Heal heal:
                    Spawn(_healIntentPrefab)?.GetComponent<UI_HealIntent>().Bind(heal);
                    break;
                case EI_Move move:
                    Spawn(_moveIntentPrefab)?.GetComponent<UI_MoveIntent>().Bind(move);
                    break;
                case EI_ApplyPerk applyPerk:
                    Spawn(_applyPerkIntentPrefab)?.GetComponent<UI_ApplyPerkIntent>().Bind(applyPerk);
                    break;
            }
        }

        // 生成图标并注入气泡宿主：图标自己承接悬停说明
        private RectTransform Spawn(RectTransform prefab_)
        {
            if (prefab_ == null) return null;
            RectTransform icon = Instantiate(prefab_, transform);
            icon.GetComponent<UI_IntentIcon>()?.Initialize(PopTextRoot);
            return icon;
        }

        private void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
        }
    }
}
