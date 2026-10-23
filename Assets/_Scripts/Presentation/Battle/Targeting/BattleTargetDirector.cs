using System.Collections.Generic;
using _DreamKeep.EngineUtility;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>瞄准中间件与目标提示驱动：整场战斗只有一支目标箭头与一个瞄准槽位，由本类独占。
    /// 手牌瞄准器在抓起 / 松手 / 取消时登记与注销，每帧由本类读取它的事实——
    /// 要不要解析目标、用不用指针指向、箭头起点在哪、箭头是否已展开——解析出"这一轮真正会命中的目标"，
    /// 把结果（目标槽位与目标集合）一次性回写，再点亮对应单位的提示框、按要求画出箭头。
    /// 落点规则一律来自手牌转交的玩法层只读查询（表现层不重写规则），指针命中判定也只在玩法层给出的存活敌人里做。
    /// 它是瞄准结果的唯一计算者与持有者，也是瞄准相关每帧的唯一驱动者。
    /// 挂在战斗根节点 GameRoot（与组合根并列，不属于任何 Canvas），由场景注册 + [Inject] 装配。</summary>
    public sealed class BattleTargetDirector : MonoBehaviour
    {
        private const int NoAimSlot = -1; // 指针没有指向任何敌人
        private const int FallbackAimSlot = 0; // 非 AnyEnemy 客户端：落点解析不读槽位，但命令需要一个合法下标，故给一个稳定默认值

        private BattleQueryDispatcher _queryDispatcher;
        private UI_ActorRoot _actorRoot;
        private UI_TargetArrow _targetArrow;
        private RectTransform _pointerPlane;
        private Camera _canvasCamera;

        // 当前瞄准客户端：单槽位——同一时刻只允许一件东西在瞄准，后登记的顶掉前一个
        private UI_HandAim _client;

        // 已点亮的目标（差分用）与箭头是否正展开：只作"状态没变就不写"的守卫
        private readonly List<Creature> _shownTargets = new();
        private bool _arrowShown;

        #region 对外暴露的装配入口与瞄准登记：登记由客户端在抓起 / 松手 / 取消时调用

        [Inject]
        public void Construct(BattleQueryDispatcher queryDispatcher_, UI_ActorRoot actorRoot_, UI_TargetArrow targetArrow_)
        {
            _queryDispatcher = queryDispatcher_;
            _actorRoot = actorRoot_;
            _targetArrow = targetArrow_;
            // 指针换算只要"同一画布平面上的任意矩形"：取箭头自身的矩形与它所在画布的相机，零 Inspector 接线
            _pointerPlane = (RectTransform)_targetArrow.transform;
            _canvasCamera = _targetArrow.GetComponentInParent<Canvas>(true).worldCamera;
        }

        /// <summary>登记当前瞄准客户端：顶掉上一个客户端，并先收起箭头与高亮（免得上一支箭头把命中色与脉冲留在展开态）。</summary>
        public void BeginAim(UI_HandAim client_)
        {
            if (ReferenceEquals(_client, client_)) return;
            ClearAim();
            _client = client_;
        }

        /// <summary>结束瞄准：只认当前客户端（非当前客户端的调用是空操作），收起箭头并熄灭全部目标提示。</summary>
        public void EndAim(UI_HandAim client_)
        {
            if (!ReferenceEquals(_client, client_)) return;
            _client = null;
            ClearAim();
        }

        #endregion

        // 每帧：没有客户端或客户端未在瞄准就收尾后早退（箭头只收一次、空高亮表不写）；
        // 否则解析本轮结果 → 一次性回写客户端 → 差分点亮 → 按需画箭头
        private void Update()
        {
            if (_client == null || !_client.IsAiming)
            {
                ClearAim();
                return;
            }

            int slot = _client.UsesPointerTargeting ? ResolveEnemySlot() : FallbackAimSlot;
            IReadOnlyList<Creature> targets = slot < 0 ? null : _client.PreviewTargets(slot);
            _client.ApplyAimResult(slot, targets);
            RefreshHighlights(targets);

            if (_client.ArrowShown)
            {
                _targetArrow.Show(_client.AimOrigin, PointerWorld(), slot >= 0);
                _arrowShown = true;
            }
            else if (_arrowShown)
            {
                _targetArrow.Hide();
                _arrowShown = false;
            }
        }

        // 指针落在哪个存活敌人的命中框内：按存活列表顺序取第一个命中，都没命中为 -1。
        // 该下标既作命令参数回写客户端，也用于同一份玩法层落点解析——提示的是谁、松手打的就是谁
        private int ResolveEnemySlot()
        {
            Vector3 pointerWorld = PointerWorld();
            IReadOnlyList<Creature> alive = _queryDispatcher.GetAliveEnemies();
            for (int i = 0; i < alive.Count; i++)
            {
                UI_ActorHoverBox box = GetHoverBox(alive[i]);
                if (!box || !box.ContainsWorldPoint(pointerWorld)) continue;
                return i;
            }

            return NoAimSlot;
        }

        // 指针在画布平面上的世界坐标：取箭头所在矩形与画布相机换算（同一画布平面上的任意矩形结果一致），
        // 无指针设备的极端情况下退回箭头自身位置
        private Vector3 PointerWorld()
        {
            Pointer pointer = Pointer.current;
            return pointer == null
                ? _pointerPlane.position
                : UiGeometry.ScreenToPlaneWorld(_pointerPlane, pointer.position.ReadValue(), _canvasCamera);
        }

        // 收尾：箭头显示过才收（Hide 内含脉冲复位），高亮按差分熄灭
        private void ClearAim()
        {
            if (_arrowShown)
            {
                _targetArrow.Hide();
                _arrowShown = false;
            }

            HideAll();
        }

        // 按目标集合变化点亮 / 熄灭：顺序由玩法层的解析确定，逐项等值即算没变；
        // 点亮的视图可能已随死亡销毁，对已销毁对象静默跳过
        private void RefreshHighlights(IReadOnlyList<Creature> targets_)
        {
            int count = targets_?.Count ?? 0;
            if (count == _shownTargets.Count)
            {
                bool same = true;
                for (int i = 0; i < count; i++)
                {
                    if (targets_[i] == _shownTargets[i]) continue;
                    same = false;
                    break;
                }

                if (same) return;
            }

            HideAll();
            if (targets_ == null) return;
            foreach (Creature creature in targets_)
            {
                UI_ActorHoverBox box = GetHoverBox(creature);
                if (!box) continue;
                box.ShowTargetTip(true);
                _shownTargets.Add(creature);
            }
        }

        // 熄灭全部提示：按登记的视图反查命中框（视图随死亡销毁时 OnDestroy 已注销登记，TryGetView 自然返回 false）
        private void HideAll()
        {
            if (_shownTargets.Count == 0) return;
            foreach (Creature creature in _shownTargets)
            {
                UI_ActorHoverBox box = GetHoverBox(creature);
                if (box) box.ShowTargetTip(false);
            }

            _shownTargets.Clear();
        }

        // 命中框查询：视图或命中框缺失一律当拿不到（视图可能已随死亡销毁，登记表里留下的是伪空引用）
        private UI_ActorHoverBox GetHoverBox(Creature creature_) =>
            _actorRoot.TryGetView(creature_, out UI_Actor view) && view != null ? view.HoverBox : null;
    }
}
