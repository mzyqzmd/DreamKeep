using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>加入指定卡：把一张配置卡烘焙成局内卡（ToMutable 后）追加进牌组，常用于塞入「伤口/失眠/贪婪」这类诅咒。</summary>
    [Serializable]
    [AddTypeMenu("Event/加入卡牌")]
    public sealed class EED_AddCard : EventEffectData
    {
        [SerializeField] private SO_Card _card; // 加入的卡（按配置烘焙）

        public SO_Card Card => _card;
    }
}
