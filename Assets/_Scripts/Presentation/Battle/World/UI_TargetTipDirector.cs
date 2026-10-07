using System.Collections.Generic;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>目标提示驱动：每帧读手牌区暴露的瞄准事实（拖拽中的牌、当前目标槽位与瞄准指针世界坐标），
    /// 用玩法层的只读预览解析出"这张牌真正会命中的目标"，再查 ActorRoot 登记的单位视图点亮对应目标、熄灭其余。
    /// 箭头瞄准（AnyEnemy 卡）时它还负责把"指针落在哪个敌人视图矩形内"解析成存活列表下标并回写手牌侧，
    /// 于是打牌命令与提示共用同一个目标；它归战斗单位侧——手牌侧只提供中立事实、不认识任何单位，落点规则也只有玩法层一份。</summary>
    public class UI_TargetTipDirector : MonoBehaviour
    {
        private UI_HandView _handView;
        private BattleContext _battleContext;
        private UI_ActorRoot _actorRoot;
        private readonly List<Creature> _shownTargets = new();
        private readonly List<UI_Actor> _shownActors = new();

        private const int DefaultAimSlot = 0; // 未在瞄准时复位用的默认目标槽位，与手牌侧的默认值同源
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

        // 每帧对账：没在瞄准一律复位目标槽位并熄灭（未拖拽时零分配早退）；瞄准中按目标集合变化重写，
        // 目标没变就一个字都不写，避免每帧无谓地开关提示
        private void Update()
        {
            UI_CardHolder aimed = _handView.AimedCard;
            if (!aimed)
            {
                _handView.SetAimSlot(DefaultAimSlot);
                HideAll();
                return;
            }

            List<Creature> targets = aimed.Card.IsAnyEnemyTarget ? ResolveArrowTargets(aimed.Card) : PreviewDragTargets(aimed.Card);
            if (SameAsShown(targets)) return;
            HideAll();
            ShowAll(targets);
        }

        // 拖拽瞄准（拖到出牌区）：落点规则只有玩法层一份，按手牌侧给出的目标槽位预览
        private List<Creature> PreviewDragTargets(CardModel card_) =>
            CardAffixResolver.PreviewTargets(_battleContext, card_, _battleContext.Player, _handView.CurrentAimSlot);

        // 箭头瞄准（AnyEnemy 卡）：先按"指针世界点落在哪个敌人视图的矩形内"定出存活列表下标并回写手牌侧，
        // 再用同一份玩法层落点解析出目标——提示的是谁，松手打的就是谁；未指向时回写 -1 并给出空集合
        private List<Creature> ResolveArrowTargets(CardModel card_)
        {
            int slot = ResolveAimSlot(_handView.AimPointerWorld);
            _handView.SetAimSlot(slot);
            return slot < 0
                ? new List<Creature>()
                : CardAffixResolver.PreviewTargets(_battleContext, card_, _battleContext.Player, slot);
        }

        // 指针指向的敌人：按存活列表顺序取第一个矩形包含指针的视图，都没命中返回 -1（视图随死亡销毁时静默跳过）
        private int ResolveAimSlot(Vector3 pointerWorld_)
        {
            List<Enemy> alive = _battleContext.GetAliveEnemies();
            for (int i = 0; i < alive.Count; i++)
            {
                if (!_actorRoot.TryGetView(alive[i].Creature, out UI_Actor view) || view == null) continue;
                if (view.ContainsWorldPoint(pointerWorld_)) return i;
            }

            return NoAimSlot;
        }

        // 目标集合比较：顺序由玩法层的解析确定，逐项等值即可
        private bool SameAsShown(List<Creature> targets_)
        {
            if (targets_.Count != _shownTargets.Count) return false;
            for (int i = 0; i < targets_.Count; i++)
                if (targets_[i] != _shownTargets[i])
                    return false;
            return true;
        }

        private void ShowAll(List<Creature> targets_)
        {
            foreach (Creature creature in targets_)
            {
                if (!_actorRoot.TryGetView(creature, out UI_Actor view) || view == null) continue;
                view.ShowTargetTip(true);
                _shownTargets.Add(creature);
                _shownActors.Add(view);
            }
        }

        // 熄灭：视图可能已随死亡销毁，对已销毁对象静默跳过（Unity 的空引用判定）
        private void HideAll()
        {
            foreach (UI_Actor actor in _shownActors)
                if (actor != null)
                    actor.ShowTargetTip(false);
            _shownActors.Clear();
            _shownTargets.Clear();
        }
    }
}
