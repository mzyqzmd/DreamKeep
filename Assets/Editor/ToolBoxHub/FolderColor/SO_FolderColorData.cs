using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ToolBoxHub.FolderColor
{
    /// <summary>
    /// 颜色映射数据（GUID -> 颜色）持久化资产。
    /// 继承 ScriptableObject，按命名规则以 SO_ 开头；懒加载单例由基座提供。
    /// </summary>
    public class SO_FolderColorData : ScriptableObjectSingleton<SO_FolderColorData>
    {
        protected override string AssetPath => "Assets/Editor/ToolBoxHub/FolderColor/FolderColorData.asset";

        [SerializeField] private List<Entry> _entries = new List<Entry>();

        [System.Serializable]
        private class Entry
        {
            public string guid;
            public string colorHtml; // 形如 "#RRGGBB"
        }

        #region 读取某 GUID 的颜色（无记录返回 null）
        public Color? GetColor(string guid_)
        {
            var e = _entries.Find(x => x.guid == guid_);
            if (e == null) return null;
            return ColorUtility.TryParseHtmlString(e.colorHtml, out var c) ? (Color?)c : null;
        }
        #endregion

        #region 写入 / 清除某 GUID 的颜色并保存
        public void SetColor(string guid_, Color color_)
        {
            var e = _entries.Find(x => x.guid == guid_);
            var html = "#" + ColorUtility.ToHtmlStringRGB(color_);
            if (e == null) _entries.Add(new Entry { guid = guid_, colorHtml = html });
            else e.colorHtml = html;
            Save();
        }

        public void RemoveColor(string guid_)
        {
            _entries.RemoveAll(x => x.guid == guid_);
            Save();
        }
        #endregion

        // 标记脏并写回磁盘
        private void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
    }
}
