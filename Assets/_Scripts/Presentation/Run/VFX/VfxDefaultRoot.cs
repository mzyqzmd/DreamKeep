using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.CommonPresentation;
using _DreamKeep.EngineUtility;
using _DreamKeep.GameRun;
using _DreamKeep.RuntimeData;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>常驻特效默认根：整局层无段锚点的特效取 Canvas_Global 中心作播放位置（战斗外玩家回血、获得卡牌、
    /// 事件/先古段受伤的全屏抖动与「失去 N」飘字）。
    /// 段内特效不走这里：战斗内的回血由段内 BE_HealthRestored 链路落在目标位置，战斗内的伤害飘字归段内弹层根。
    /// 场景里挂在 GameRoot 上，_root 接 Canvas_Global；受伤抖动的驱动组件在装配时挂到常驻主相机，随本组件销毁移除。</summary>
    public sealed class VfxDefaultRoot : MonoBehaviour
    {
        [Tooltip("默认播放父级：把 Canvas_Global 拖入")] [SerializeField]
        private RectTransform _root;

        [Tooltip("回血特效缩放")] [SerializeField] private float _healScale = 1f;

        [Header("获得卡牌")] [Tooltip("飞行落点：顶栏卡组按钮，展示卡飞入它表示收入牌库；未接线则整段静默跳过")] [SerializeField]
        private RectTransform _flyTarget;

        [Tooltip("获得卡演出缩放")] [SerializeField] private float _gainCardScale = 1f;

        [Header("受伤演出")] [Tooltip("失去生命的飘字颜色：与战斗内失去生命同口径，比普通伤害更深")] [SerializeField]
        private Color _lostHpColor = new(0.62f, 0.05f, 0.05f);

        private RunState _run;
        private BattleSession _session;
        private VfxFactory _vfxFactory;
        private PopFactory _popFactory;
        private ScreenShakeDriver _shakeDriver;
        private readonly Queue<CardModel> _pendingGains = new(); // 同帧多卡排队：逐张串演不叠影
        private bool _playingGain; // 上一张还在演：后续入队等待回调接力

        private void OnDestroy()
        {
            if (_run == null) return; // 容器构建失败时 Construct 未跑过，别在场景卸载时补一刀空引用
            _run.Player.Creature.Healed -= OnHealed;
            _run.Player.Creature.HpLost -= OnHpLost;
            _run.Player.CardAdded -= OnCardAdded;
            if (_shakeDriver) Destroy(_shakeDriver); // 驱动挂在常驻主相机上，不随本场景卸载自动消失
        }

        // 订阅放注入回调：RunState 与常驻组件同生共死，退订只为不给下一局留订阅
        [Inject]
        public void Construct(RunState run_, BattleSession session_,
            VfxFactory vfxFactory_, PopFactory popFactory_, ScreenShakeDriver shakeDriver_)
        {
            _run = run_;
            _session = session_;
            _vfxFactory = vfxFactory_;
            _popFactory = popFactory_;
            _run.Player.Creature.Healed += OnHealed;
            _run.Player.Creature.HpLost += OnHpLost;
            _run.Player.CardAdded += OnCardAdded;
            _shakeDriver = shakeDriver_;
        }

        // 战斗中回血归段内链路（目标位置），这里只管非战斗；领奖期（段在但战斗已结束）按非战斗
        private void OnHealed(int amount_)
        {
            if (_session.IsFighting || !_root) return;
            _vfxFactory.PlayHeal(_root, _root.TransformPoint(
                new Vector3(0, _root.rect.yMin, 0)), _healScale);
        }

        // 非战斗受伤：先震屏、抖动收尾再弹「失去 N」飘字；战斗内归段内伤害链路，这里不重复
        private void OnHpLost(int amount_)
        {
            if (_session.IsFighting || !_root) return;
            float duration = _shakeDriver ? _shakeDriver.Play() : 0f;
            PopLostHpAsync(amount_, duration).Forget();
        }

        // 抖动与飘字都不阻塞域流程：延迟只在这里排队，结果页照常同帧翻页
        private async UniTask PopLostHpAsync(int amount_, float delay_)
        {
            if (delay_ > 0f)
            {
                bool canceled = await UniTask.Delay(TimeSpan.FromSeconds(delay_),
                    cancellationToken: destroyCancellationToken).SuppressCancellationThrow();
                if (canceled) return;
            }

            if (!_root) return;
            _popFactory.SpawnNumber($"失去 {amount_}", _lostHpColor, ENumberPopTipType.Toss, _root,
                UiGeometry.AnchorPoint(_root, EUiAnchor.MiddleCenter));
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