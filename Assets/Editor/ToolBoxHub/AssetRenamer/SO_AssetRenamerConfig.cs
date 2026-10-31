using UnityEditor;
using UnityEngine;

namespace ToolBoxHub.AssetRenamer
{
    /// <summary>
    /// 资源重命名配置：懒加载单例，持久化重命名规则。
    /// </summary>
    public class SO_AssetRenamerConfig : ScriptableObjectSingleton<SO_AssetRenamerConfig>
    {
        protected override string AssetPath => "Assets/Editor/ToolBoxHub/AssetRenamer/SO_AssetRenamerConfig.asset";

        [SerializeField] private string _prefix = "";
        [SerializeField] private string _suffix = "";
        [SerializeField] private string _find = "";
        [SerializeField] private string _replace = "";
        [SerializeField] private bool _useNumbering;
        [SerializeField] private int _startNumber = 1;
        [SerializeField] private ECaseMode _caseMode = ECaseMode.None;

        public string Prefix => _prefix;
        public string Suffix => _suffix;
        public string Find => _find;
        public string Replace => _replace;
        public bool UseNumbering => _useNumbering;
        public int StartNumber => _startNumber;
        public ECaseMode CaseMode => _caseMode;

        #region 配置写回
        public void Set(string prefix_, string suffix_, string find_, string replace_, bool useNumbering_, int startNumber_, ECaseMode caseMode_)
        {
            _prefix = prefix_;
            _suffix = suffix_;
            _find = find_;
            _replace = replace_;
            _useNumbering = useNumbering_;
            _startNumber = startNumber_;
            _caseMode = caseMode_;
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
        #endregion
    }
}
