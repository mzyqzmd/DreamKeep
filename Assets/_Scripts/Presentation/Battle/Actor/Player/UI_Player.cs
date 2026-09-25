using _DreamKeep.GameBattle;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>玩家视图：战斗单位共享表现（UI_Actor）的玩家侧装配点——绑定玩家生物数据、注册域事件，
    /// 并把组合根上的两个 Perk 预制体下发到信息面板；敌人视图落地后按同样的三步装配。</summary>
    public class UI_Player : UI_Actor
    {
        [Inject]
        public void Construct(BattleContext battleContext_, BattleEventDispatcher eventDispatcher_,
            UI_PerkSlot perkSlotPrefab_, UI_PerkPopTip perkPopTipPrefab_)
        {
            Bind(battleContext_.Player.Creature);
            _infoPanel.Init(BoundCreature, perkSlotPrefab_, perkPopTipPrefab_);
            RegisterActorEvents(eventDispatcher_);
        }
    }
}
