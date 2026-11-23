using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>风箱：战斗首回合把即将抽到的初始手牌全部升级。
    /// 张数不配置——取战斗态里的每回合抽牌数，避免与角色配置漂移。</summary>
    [CreateAssetMenu(fileName = "Bellows", menuName = "DreamKeep/Data/Relic/风箱")]
    public sealed class SO_Bellows : SO_Relic
    {
    }
}
