using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Presentation
{
    /// <summary>玩家单位视图：全场唯一、由生成侧在 Instantiate 后值传递装配。玩家永远没有意图、也不参与阵型移动，
    /// 故这里只有装配入口；共享的面板与事件在基类，玩家专属表现将来也落在本类。</summary>
    public class UI_Player : UI_Actor
    {
        #region 对外暴露的装配入口：由生成侧在 Instantiate 后值传递装配

        public void Bind(BattleEventDispatcher dispatcher_, Player player_, ActorViewRegistry registry_) =>
            base.Bind(dispatcher_, player_.Creature, registry_);

        #endregion
    }
}
