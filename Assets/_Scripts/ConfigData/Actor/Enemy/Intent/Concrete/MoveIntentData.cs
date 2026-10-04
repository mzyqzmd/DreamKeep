using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>前进意图配置：无参数，执行时敌人向玩家方向前进一个槽位。</summary>
    [Serializable]
    [AddTypeMenu("Intent/前进")]
    public class MoveIntentData : IntentData
    {
        public override string GetDescription() =>
            "这名敌人即将向玩家方向前进一个槽位,处在最前槽位时再向前回到后方槽位,处于最前槽位时触发特殊意图";
    }
}