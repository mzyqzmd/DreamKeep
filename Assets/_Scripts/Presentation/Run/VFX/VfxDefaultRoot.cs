using System.Collections.Generic;
using _DreamKeep.CommonPresentation;
using _DreamKeep.EngineUtility;
using _DreamKeep.GameRun;
using _DreamKeep.RuntimeData;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>常驻特效默认根：整局层无段锚点的特效取 Canvas_Global 中心作播放位置（战斗外玩家回血、获得卡牌）。
    /// 段内特效不走这里：战斗内的回血由段内 BE_HealthRestored 链路落在目标位置，休息/战斗各有自己的 VFX Root。
    /// 场景里挂在 GameRoot 上，_root 接 Canvas_Global。</summary>
    public sealed class VfxDefaultRoot : MonoBehaviour
    {
        [Tooltip("默认播放父级：把 Canvas_Global 拖入")] [SerializeField]
        private RectTransform _root;

        [Tooltip("回血特效缩放")] [SerializeField] private float _healScale = 1f;

        [Header("获得卡牌")] [Tooltip("飞行落点：顶栏卡组按钮，展示卡飞入它表示收入牌库；未接线则整段静默跳过")] [SerializeField]
        private RectTransform _flyTarget;

        [Tooltip("获得卡演出缩放")] [SerializeField] private float _gainCardScale = 1f;

        private RunState _run;
        private BattleSession _session;
        private VfxFactory _vfxFactory;
        private readonly Queue<CardModel> _pendingGains = new(); // 同帧多卡排队：逐张串演不叠影
        private bool _playingGain; // 上一张还在演：后续入队等待回调接力

        private void OnDestroy()
        {
            if (_run == null) return; // 容器构建失败时 Construct 未跑过，别在场景卸载时补一刀空引用
            _run.Player.Creature.Healed -= OnHealed;
            _run.Player.CardAdded -= OnCardAdded;
        }

        // 订阅放注入回调：RunState 与常驻组件同生共死，退订只为不给下一局留订阅
        [Inject]
        public void Construct(RunState run_, BattleSession session_, VfxFactory vfxFactory_)
        {
            _run = run_;
            _session = session_;
            _vfxFactory = vfxFactory_;
            _run.Player.Creature.Healed += OnHealed;
            _run.Player.CardAdded += OnCardAdded;
        }

        // 战斗中回血归段内链路（目标位置），这里只管非战斗；领奖期（段在但战斗已结束）按非战斗
        private void OnHealed(int amount_)
        {
            if (_session.IsFighting || !_root) return;
            _vfxFactory.PlayHeal(_root, _root.TransformPoint(
                new Vector3(0, _root.rect.yMin, 0)), _healScale);
        }

        // 落点未接线则整段静默跳过：特效通道沿"没接就不播"口径，不报错不卡入组
        private void OnCardAdded(CardModel card_)
        {
            if (!_root || !_flyTarget) return;
            _pendingGains.Enqueue(card_);
            if (!_playingGain) PlayNextGain();
        }

        // 队列串演：播完回调回来再取下一张，同帧两张连获不叠影
        private void PlayNextGain()
        {
            if (_pendingGains.Count == 0)
            {
                _playingGain = false;
                return;
            }

            _playingGain = true;
            _vfxFactory.PlayGainCard(_root, _root.TransformPoint(Vector3.zero), _gainCardScale,
                UiGeometry.AnchorPoint(_flyTarget, EUiAnchor.MiddleCenter), _pendingGains.Dequeue(),
                PlayNextGain);
        }
    }
}
