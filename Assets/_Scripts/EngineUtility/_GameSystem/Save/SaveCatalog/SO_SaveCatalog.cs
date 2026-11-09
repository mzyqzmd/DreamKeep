using System.Collections.Generic;
using UnityEngine;

namespace _DreamKeep.EngineUtility
{
    /// <summary>存档资产薄目录：编辑器烘焙「资产 GUID ↔ 资产引用」两张平行表，运行期双向查表。
    /// 只存 ScriptableObject 基类引用，使本程序集不必依赖任何具体配置类型；资产改名或移动都不影响旧档。</summary>
    public sealed class SO_SaveCatalog : ScriptableObject
    {
        [SerializeField] private List<string> _guids = new();
        [SerializeField] private List<ScriptableObject> _assets = new();

        private Dictionary<string, ScriptableObject> _byGuid;
        private Dictionary<ScriptableObject, string> _guidByAsset;

        #region 按 GUID 取资产：缺项返回 null，由调用方跳过，不让整份档失败

        public T Find<T>(string guid_) where T : ScriptableObject
        {
            if (string.IsNullOrEmpty(guid_)) return null;
            EnsureIndex();
            return _byGuid.TryGetValue(guid_, out ScriptableObject asset) ? asset as T : null;
        }

        #endregion

        #region 按资产取 GUID：空槽返回 null，写盘时据此保留空位

        public string GuidOf(ScriptableObject asset_)
        {
            if (asset_ == null) return null;
            EnsureIndex();
            return _guidByAsset.TryGetValue(asset_, out string guid) ? guid : null;
        }

        #endregion

        private void EnsureIndex()
        {
            if (_byGuid != null) return;
            _byGuid = new Dictionary<string, ScriptableObject>();
            _guidByAsset = new Dictionary<ScriptableObject, string>();
            int count = Mathf.Min(_guids.Count, _assets.Count);
            for (int i = 0; i < count; i++)
            {
                ScriptableObject asset = _assets[i];
                if (asset == null || string.IsNullOrEmpty(_guids[i])) continue;
                _byGuid[_guids[i]] = asset;
                _guidByAsset[asset] = _guids[i];
            }
        }
    }
}
