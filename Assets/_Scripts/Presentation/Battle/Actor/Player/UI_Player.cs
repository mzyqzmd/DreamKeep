using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Presentation
{
    /// <summary>玩家单位视图：全场唯一、由生成侧在 Instantiate 后值传递装配；永远没有意图、也不参与阵型移动，
    /// 面板与演出全部走基类（受击/攻击/死亡由基类公约常量 hurt/attack/die 覆盖）。
    /// 待机等自有动画名对齐 ironclad 骨架（二进制提取，待 SkeletonDataAsset Inspector 核对）。</summary>
    public class UI_Player : UI_Actor
    {
        // ironclad 自有动画（二进制提取）：idle_loop/relaxed_loop/relaxed_loop2，待机暂取 idle_loop 待核对
        protected override string StandAnim => "idle_loop";

        #region 对外暴露的装配入口：由生成侧在 Instantiate 后值传递装配

        public void Bind(BattleEventDispatcher eventDispatcher_, Player player_)
            => base.Bind(eventDispatcher_, player_.Creature);

        #endregion
    }
}
