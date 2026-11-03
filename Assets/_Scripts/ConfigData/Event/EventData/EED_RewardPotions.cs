using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>药水奖励：无论几瓶都弹奖励面板逐条领取（药水槽有上限，面板条目槽满会解锁让玩家先喝再领，绝不盲目入槽）。</summary>
    [Serializable]
    [AddTypeMenu("Event/药水奖励")]
    public sealed class EED_RewardPotions : EventEffectData
    {
        [SerializeField] private SO_PotionPool _pool; // 药水来源池
        [SerializeField] private int _count; // 药水瓶数（池内不重复抽取）
        [SerializeField] private string _panelTitle = "药水奖励"; // 奖励面板标题

        public SO_PotionPool Pool => _pool;
        public int Count => _count;
        public string PanelTitle => _panelTitle;
    }
}
