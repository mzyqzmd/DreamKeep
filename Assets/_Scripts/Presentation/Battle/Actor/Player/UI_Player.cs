using _DreamKeep.GameBattle;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>玩家视图：战斗单位共享表现（UI_Actor）的玩家侧装配点——绑定玩家生物数据、注册域事件；
    /// 敌人视图落地后按同样方式装配。</summary>
    public class UI_Player : UI_Actor
    {
        public override void Construct(BattleContext battleContext_, BattleEventDispatcher eventDispatcher_)
        {
            _boundCreature = battleContext_.Player.Creature;
            base.Construct(battleContext_, eventDispatcher_);
        }
    }
}