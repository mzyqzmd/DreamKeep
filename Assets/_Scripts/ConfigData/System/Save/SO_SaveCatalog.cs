using System.Collections.Generic;
using UnityEngine;

namespace _DreamKeep.ConfigData
{
    /// <summary>存档配置目录：Editor 烘焙「资产 GUID ↔ 配置资产」两张平行表，运行期按 GUID 反查配置，
    /// 让存档 DTO 只写稳定 ID、不写资产引用；配置资产改名或移动路径都不会让旧档失效。</summary>
    public sealed class SO_SaveCatalog : ScriptableObject
    {
        [SerializeField] private List<string> _guids = new();
        [SerializeField] private List<ScriptableObject> _assets = new();

        private Dictionary<string, ScriptableObject> _byGuid;
        private Dictionary<ScriptableObject, string> _guidByAsset;

        #region 按 GUID 取配置：缺项返回 null，由调用方跳过并告警，不让整份档失败

        public ScriptableObject Find(string guid_)
        {
            if (string.IsNullOrEmpty(guid_)) return null;
            if (_byGuid == null) BuildIndex();
            return _byGuid.TryGetValue(guid_, out ScriptableObject asset) ? asset : null;
        }

        public T Find<T>(string guid_) where T : ScriptableObject => Find(guid_) as T;

        #endregion

        #region 按配置资产取 GUID：空引用记 null，药水槽空位由此落档

        public string GuidOf(ScriptableObject asset_)
        {
            if (asset_ == null) return null;
            if (_guidByAsset == null) BuildIndex();
            return _guidByAsset.TryGetValue(asset_, out string guid) ? guid : null;
        }

        #endregion

        private void BuildIndex()
        {
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
