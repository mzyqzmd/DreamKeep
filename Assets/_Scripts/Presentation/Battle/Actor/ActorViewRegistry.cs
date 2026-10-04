using System.Collections.Generic;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Presentation
{
    /// <summary>战斗单位视图注册表：生物 → 单位视图。敌人视图由生成侧运行时创建，目标提示要按玩法层给的目标找视图只能靠它；
    /// 单位在装配时自登记、销毁时注销（覆盖敌人死亡自我销毁）。纯数据，无 Unity 依赖。</summary>
    public class ActorViewRegistry
    {
        private readonly Dictionary<Creature, UI_Actor> _views = new();

        #region 对外暴露的登记与查询

        public void Register(Creature creature_, UI_Actor view_) => _views[creature_] = view_;

        public void Unregister(Creature creature_) => _views.Remove(creature_);

        public bool TryGetView(Creature creature_, out UI_Actor view_) => _views.TryGetValue(creature_, out view_);

        #endregion
    }
}
