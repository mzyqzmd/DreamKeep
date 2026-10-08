using System.Collections.Generic;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>目标提示驱动：每帧读手牌区暴露的瞄准事实（瞄准中的牌、目标槽位、指针世界坐标），用玩法层的只读预览
    /// 解析出"这张牌真正会命中的目标"，再查 ActorRoot 登记的单位视图点亮对应目标、熄灭其余；箭头瞄准时它还负责把
    /// "指针落在哪个敌人视图矩形内"解析成存活列表下标并回写手牌侧，于是打牌命令与提示共用同一个目标。
    /// 它归战斗单位侧：手牌侧只提供中立事实、不认识任何单位，落点规则也只有玩法层一份。</summary>
    public class UI_TargetDirector : MonoBehaviour
    {
        private UI_HandView _handView;
        private BattleContext _battleContext;
        private UI_ActorRoot _actorRoot;
        private readonly List<Creature> _shownTargets = new();

        private const int NoAimSlot = -1; // 指针没有指向任何敌人

        #region 对外暴露的装配入口：容器注入手牌视图、战斗态与单位区

        [Inject]
        public void Construct(UI_HandView handView_, BattleContext battleContext_, UI_ActorRoot actorRoot_)
        {
            _handView = handView_;
            _battleContext = battleContext_;
            _actorRoot = actorRoot_;
        }

        #endregion

        // 每帧对账：没在瞄准就复位目标槽位并熄灭（未拖拽时零分配早退）；瞄准中按目标集合变化重写，集合没变就一个字都不写
        private void Update()
        {
            UI_CardHolder aimed = _handView.AimedCard;
            if (!aimed)
            {
                _handView.SetAimSlot(UI_HandView.DefaultAimSlot);
                HideAll();
                return;
            }

            List<Creature> targets;
            if (aimed.Card.IsAnyEnemyTarget)
            {
                // 箭头瞄准：指针落在哪个敌人视图的矩形内就取哪个（按存活列表顺序，都没命中为 -1），
                // 按该下标回写手牌侧，再用同一份玩法层落点解析——提示的是谁，松手打的就是谁
                Vector3 pointerWorld = _handView.PointerWorldPosition();
                List<Enemy> alive = _battleContext.GetAliveEnemies();
                int slot = NoAimSlot;
                for (int i = 0; i < alive.Count; i++)
                {
                    if (!_actorRoot.TryGetView(alive[i].Creature, out UI_Actor view) || view == null) continue;
                    if (!view.ContainsWorldPoint(pointerWorld)) continue;
                    slot = i;
                    break;
                }

                _handView.SetAimSlot(slot);
                targets = slot < 0
                    ? new List<Creature>()
                    : CardAffixResolver.PreviewTargets(_battleContext, aimed.Card, _battleContext.Player, slot);
            }
            else
            {
                // 拖拽瞄准（拖到出牌区）：落点规则只有玩法层一份，按手牌侧给出的目标槽位预览
                targets = CardAffixResolver.PreviewTargets(_battleContext, aimed.Card, _battleContext.Player,
                    _handView.CurrentAimSlot);
            }

            // 目标集合比较：顺序由玩法层的解析确定，逐项等值即可；没变就早退，不做任何开关
            if (targets.Count == _shownTargets.Count)
            {
                bool same = true;
                for (int i = 0; i < targets.Count; i++)
                {
                    if (targets[i] == _shownTargets[i]) continue;
                    same = false;
                    break;
                }

                if (same) return;
            }

            // 集合变了才重写：点亮的视图可能已随死亡销毁，对已销毁对象静默跳过
            HideAll();
            foreach (Creature creature in targets)
            {
                if (!_actorRoot.TryGetView(creature, out UI_Actor view) || view == null) continue;
                view.ShowTargetTip(true);
                _shownTargets.Add(creature);
            }
        }

        // 熄灭全部提示：按登记的视图反查（视图随死亡销毁时 OnDestroy 已注销登记，TryGetView 自然返回 false）
        private void HideAll()
        {
            foreach (Creature creature in _shownTargets)
                if (_actorRoot.TryGetView(creature, out UI_Actor view) && view != null)
                    view.ShowTargetTip(false);
            _shownTargets.Clear();
        }
    }
}
