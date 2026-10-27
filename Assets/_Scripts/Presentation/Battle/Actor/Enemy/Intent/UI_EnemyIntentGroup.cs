using System.Collections.Generic;
using _DreamKeep.GameBattle;
using _DreamKeep.Presentation.Intent;
using _DreamKeep.RuntimeData;
using _DreamKeep.EngineUtility;
using UnityEngine;
using VContainer;
using VContainer.Unity; // InjectGameObject 扩展方法住在这里

namespace _DreamKeep.Presentation
{
    /// <summary>敌人意图预告组：把展示意图翻译成头顶图标串并分发视觉绑定，负责生成与清空；
    /// 悬停说明由每个图标自己承接（悬停哪一张弹哪一张，气泡内容在图标 Bind 时喂给它的气泡发起器）；
    /// 刷新归持有它的敌人视图 UI_Enemy，自己不订阅域事件。</summary>
    public sealed class UI_EnemyIntentGroup : MonoBehaviour
    {
        [Header("意图预制体")] [SerializeField] private UI_AttackIntent _attackIntentPrefab;
        [SerializeField] private UI_BlockIntent _blockIntentPrefab;
        [SerializeField] private UI_HealIntent _healIntentPrefab;
        [SerializeField] private UI_MoveIntent _moveIntentPrefab;
        [SerializeField] private UI_ApplyPerkIntent _applyPerkIntentPrefab;

        // 当前展示的图标（按排列顺序）：归还只认自己这份名单，不扫子物体——池的空闲实例也挂在本组名下
        private readonly List<UI_Intent> _icons = new();
        private MonoObjectPool<UI_Intent> _iconPool;

        [Inject]
        public void Construct(IObjectResolver resolver_)
        {
            _iconPool = new MonoObjectPool<UI_Intent>(transform,
                onCreate_: icon_ => resolver_.InjectGameObject(icon_.gameObject));
        }

        #region 对外展示：按展示意图整组重建头顶图标

        /// <summary>重建图标组：旧图标整组归还池（归还即失活，图标自己的气泡发起器顺带收掉气泡），再按意图类型递归生成并分发绑定，null 即清空；
        /// 指针下一帧重新进入新图标再弹，最多一帧闪烁。</summary>
        public void Show(EnemyIntent intent_)
        {
            for (int i = 0; i < _icons.Count; i++) _iconPool.Release(_icons[i]);
            _icons.Clear();
            if (intent_ != null) Append(intent_);
        }

        /// <summary>刷新数值预览（敌人视图每帧对账调用）：对当前攻击意图逐个重算修正后伤害（等值短路在图标内，未变零写入）。</summary>
        public void RefreshPreviews(BattleQueryDispatcher queryDispatcher_, Creature source_)
        {
            for (int i = 0; i < _icons.Count; i++)
                if (_icons[i] is UI_AttackIntent attack)
                    attack.RefreshPreview(queryDispatcher_, source_);
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
                    Spawn(_attackIntentPrefab)?.Bind(damage);
                    break;
                case EI_Block block:
                    Spawn(_blockIntentPrefab)?.Bind(block);
                    break;
                case EI_Heal heal:
                    Spawn(_healIntentPrefab)?.Bind(heal);
                    break;
                case EI_Move move:
                    Spawn(_moveIntentPrefab)?.Bind(move);
                    break;
                case EI_ApplyPerk applyPerk:
                    Spawn(_applyPerkIntentPrefab)?.Bind(applyPerk);
                    break;
            }
        }

        // 取图标：图标自己承接悬停说明（内容在它的 Bind 里喂给气泡发起器）；
        // 取出的实例在池里埋过兄弟位，先挪到末尾再登记，排列顺序即生成顺序
        private T Spawn<T>(T prefab_) where T : UI_Intent
        {
            if (prefab_ == null) return null;
            T icon = (T)_iconPool.Get(prefab_);
            icon.transform.SetAsLastSibling();
            _icons.Add(icon);
            return icon;
        }
    }
}