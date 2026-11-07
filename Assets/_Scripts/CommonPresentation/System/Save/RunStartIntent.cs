using UnityEngine;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>跨 Scene_MainMenu 重载的一次性启动参数：用 PlayerPrefs 承载，读取即清。
    /// 静态字段会在场景 / 域重载时被清空，PlayerPrefs 不会；本类只承载枚举值，不是服务定位器。</summary>
    public static class RunStartIntent
    {
        private const string Key = "DreamKeep.RunStartIntent";

        public static void Set(ESaveStartIntent intent_) => PlayerPrefs.SetInt(Key, (int)intent_);

        public static ESaveStartIntent Consume()
        {
            ESaveStartIntent intent = (ESaveStartIntent)PlayerPrefs.GetInt(Key, (int)ESaveStartIntent.None);
            PlayerPrefs.DeleteKey(Key);
            return intent;
        }
    }
}
