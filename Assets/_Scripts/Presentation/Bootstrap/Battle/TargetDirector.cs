using System.Collections.Generic;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>目标提示驱动：每帧读手牌区暴露的瞄准事实（瞄准中的牌、指针世界坐标），用上行门面的只读查询解析出
    /// "这张牌真正会命中的目标"，把结果（目标槽位与目标集合）一次性下发给手牌区并连同驱动它刷新瞄准相关的一切，
    /// 再查 ActorRoot 登记的单位视图、由它的命中框点亮对应目标、熄灭其余；
    /// 箭头瞄准时它还负责把"指针落在哪个敌人命中框内"解析成存活列表下标。
    /// 它是瞄准结果的唯一计算者与持有者，也是瞄准相关每帧的唯一驱动者：手牌区只接收、不推导，也不反向认识本类，
    /// 因而不存在"两个组件谁先 Update"的次序之争。它归战斗单位侧：手牌侧只提供中立事实、不认识任何单位，
    /// 落点规则也只有玩法层一份。挂在战斗根节点 GameRoot（与组合根并列，不属于任何 Canvas），由场景注册 + [Inject] 装配。</summary>
    public sealed class TargetDirector : MonoBehaviour
    {
        private const int NoAimSlot = -1; // 指针没有指向任何敌人
        private const int FallbackAimSlot = 0; // 非 AnyEnemy 卡：落点解析不读槽位，但打牌命令需要一个合法下标，故给一个稳定默认值

        private UI_HandView _handView;
        private BattleQueryDispatcher _queryDispatcher;
        private UI_ActorRoot _actorRoot;
        private readonly List<Creature> _shownTargets = new();

        // 瞄准结果的权威值：槽位（存活敌人列表下标）与目标集合。每帧算完一次性下发给手牌侧，
        // 它只作同步副本、只读不写、不自行推导——依赖方向恒为"本驱动认识手牌，手牌不认识本驱动"
        private int _aimSlot = NoAimSlot;
        private IReadOnlyList<Creature> _targets;

        #region 对外暴露的装配入口：容器注入手牌视图、查询门面与单位区

        [Inject]
        public void Construct(UI_HandView handView_, BattleQueryDispatcher queryDispatcher_, UI_ActorRoot actorRoot_)
        {
            _handView = handView_;
            _queryDispatcher = queryDispatcher_;
            _actorRoot = actorRoot_;
        }

        #endregion

        // 每帧对账：先算本轮瞄准结果（槽位与目标集合）并一次性下发给手牌区——下发会连同驱动它刷新瞄准相关的一切
        //（兜底取消、箭头位姿与颜色、整手数值预览），故箭头命中色与卡面数值用的就是当帧刚算出的结果；
        // 之后按目标集合变化点亮/熄灭（集合没变就一个字都不写）
        private void Update()
        {
            ResolveAimResult();
            _handView.ApplyAimResult(_aimSlot, _targets);
            RefreshHighlights();
        }

        // 解析本轮瞄准：未瞄准、或箭头没指向任何敌人时都解析为"无目标"（槽位 -1、集合 null）；
        // 非 AnyEnemy 卡用占位槽位，落点规则仍由玩法层解析
        private void ResolveAimResult()
        {
            UI_CardBattle aimed = _handView.AimedCard;
            if (!aimed)
            {
                _aimSlot = NoAimSlot; // 没在瞄准：槽位复位，同时清掉上一次的目标集合引用
                _targets = null;
                return;
            }

            if (aimed.Card.IsAnyEnemyTarget)
            {
                // 箭头瞄准：指针落在哪个敌人的命中框内就取哪个（按存活列表顺序，都没命中为 -1）。
                // 该下标既作打牌参数下发，也用于同一份玩法层落点解析——提示的是谁，松手打的就是谁
                Vector3 pointerWorld = _handView.PointerWorldPosition();
                IReadOnlyList<Creature> alive = _queryDispatcher.GetAliveEnemies();
                _aimSlot = NoAimSlot;
                for (int i = 0; i < alive.Count; i++)
                {
                    UI_ActorHoverBox box = GetHoverBox(alive[i]);
                    if (!box || !box.ContainsWorldPoint(pointerWorld)) continue;
                    _aimSlot = i;
                    break;
                }

                // 指针不在任何敌人命中框内：本轮没有目标（与"解析出空集"等价，省一张空表）
                _targets = _aimSlot < 0 ? null : _queryDispatcher.PreviewTargets(aimed.Card, _aimSlot);
                return;
            }

            // 拖拽瞄准（拖到出牌区）：落点规则只有玩法层一份，本类现算后同样下发
            _aimSlot = FallbackAimSlot;
            _targets = _queryDispatcher.PreviewTargets(aimed.Card, FallbackAimSlot);
        }

        // 按目标集合变化点亮 / 熄灭：顺序由玩法层的解析确定，逐项等值即算没变；
        // 点亮的视图可能已随死亡销毁，对已销毁对象静默跳过
        private void RefreshHighlights()
        {
            int count = _targets?.Count ?? 0;
            if (count == _shownTargets.Count)
            {
                bool same = true;
                for (int i = 0; i < count; i++)
                {
                    if (_targets[i] == _shownTargets[i]) continue;
                    same = false;
                    break;
                }

                if (same) return;
            }

            HideAll();
            if (_targets == null) return;
            foreach (Creature creature in _targets)
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
