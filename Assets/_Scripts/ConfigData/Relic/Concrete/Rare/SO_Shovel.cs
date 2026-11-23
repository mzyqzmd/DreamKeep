using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>铲子：被动工具遗物——持有后在休息处解锁「挖掘」选项，可从未拥有的遗物中挖取一件。
    /// 本身不提供任何战斗效果，纯粹作为休息处挖掘选项的资格判定。</summary>
    [CreateAssetMenu(fileName = "Shovel", menuName = "DreamKeep/Data/Relic/铲子")]
    public sealed class SO_Shovel : SO_Relic
    {
    }
}
