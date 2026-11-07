using System;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>意图配置基类：只描述"做什么、做多少"，显示文案由运行时意图提供；
    /// _speechLine 是执行该意图时敌人说的话，留空则保持沉默。</summary>
    [Serializable]
    public abstract class IntentData
    {
        [Tooltip("执行该意图时说的台词，留空则沉默")] [SerializeField]
        private string _speechLine;

        public string SpeechLine => _speechLine;
    }
}
