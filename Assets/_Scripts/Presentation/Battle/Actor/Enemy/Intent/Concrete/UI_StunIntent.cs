using _DreamKeep.RuntimeData;

namespace _DreamKeep.Presentation
{
    /// <summary>击晕意图图标：纯图标展示，说明（本回合无法行动、结束后恢复原意图）由悬停说明气泡给出。</summary>
    public sealed class UI_StunIntent : UI_Intent
    {
        /// <summary>绑定击晕意图：视觉暂无专属内容，只需登记意图供悬停取说明。</summary>
        public void Bind(EI_Stun stun_) => BindIntent(stun_);
    }
}
