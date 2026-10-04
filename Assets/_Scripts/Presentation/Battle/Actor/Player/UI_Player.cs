using _DreamKeep.GameBattle;
using UnityEngine;
using VContainer;

namespace _DreamKeep.Presentation
{
    /// <summary>玩家单位视图：全场唯一、由容器注入后自绑玩家生物。玩家永远没有意图、也不参与阵型移动，
    /// 故这里只有装配入口；共享的面板与事件在基类，玩家专属表现将来也落在本类。</summary>
    public class UI_Player : UI_Actor
    {
        #region 对外暴露的装配入口：场景实例经容器注入自绑玩家生物

        [Inject]
        public void Construct(BattleContext battleContext_, BattleEventDispatcher eventDispatcher_, ActorViewRegistry registry_) =>
            Bind(eventDispatcher_, battleContext_.Player.Creature, registry_);

        #endregion
    }
}
