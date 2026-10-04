using _DreamKeep.RuntimeData;
using UnityEngine;

namespace _DreamKeep.Presentation.Intent
{
    /// <summary>格挡意图图标：纯图标展示，数值不对外展示（占位视图，将来有专属表现再补）。</summary>
    public class UI_BlockIntent : UI_IntentIcon
    {
        /// <summary>绑定格挡意图：视觉暂无专属内容，只需登记意图供悬停取说明。</summary>
        public void Bind(EI_Block block_) => BindIntent(block_);
    }
}
