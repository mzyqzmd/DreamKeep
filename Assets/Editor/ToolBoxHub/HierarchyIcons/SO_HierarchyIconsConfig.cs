using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ToolBoxHub.HierarchyIcons
{
    /// <summary>
    /// 层级图标规则配置（ScriptableObject）：存储组件类型 -> 图标映射及全局开关，持久化走 .asset。
    /// </summary>
    public class SO_HierarchyIconsConfig : ScriptableObjectSingleton<SO_HierarchyIconsConfig>
    {
        protected override string AssetPath => "Assets/Editor/ToolBoxHub/HierarchyIcons/SO_HierarchyIconsConfig.asset";

        [SerializeField] private bool _enabled = true;
        [SerializeField] private List<IconRule> _rules = new();

        public bool Enabled { get => _enabled; set => _enabled = value; }
        public IReadOnlyList<IconRule> GetRules() => _rules;

        private static readonly string[] _presetTypeNames =
        {
            "Camera", "Light", "MeshRenderer", "MeshFilter",
            "SkinnedMeshRenderer", "Rigidbody", "AudioSource", "AudioListener",
            "ParticleSystem", "Animator", "Animation", "BoxCollider",
            "SphereCollider", "CapsuleCollider", "Canvas", "RectTransform",
            "Rigidbody2D", "BoxCollider2D", "CircleCollider2D", "PolygonCollider2D",
            "CapsuleCollider2D", "SpriteRenderer"
        };

        [Serializable]
        /// <summary>单条图标规则：组件类型名 → 图标（留空则用组件内置图标）。</summary>
        public class IconRule
        {
            public string ruleName;   // 组件类型全名，Type.GetType 解析后 GetComponent 匹配
            public Texture2D icon;    // 行右侧图标；为空时取组件内置图标
            public bool enabled;      // 规则独立开关，不删除
        }

        // 首次加载且规则为空时预填内置预设
        protected override void OnLoaded()
        {
            if (GetRules().Count == 0) ApplyPresets();
        }

        #region 规则增删与持久化
        public void AddRule(string ruleName_, Texture2D icon_)
        {
            if (_rules.Find(x => x.ruleName == ruleName_) == null)
                _rules.Add(new IconRule() { ruleName = ruleName_, icon = icon_, enabled = true });
            Save();
        }

        public void ApplyPresets()
        {
            foreach (var name in _presetTypeNames)
                if (_rules.Find(x => x.ruleName == name) == null)
                    _rules.Add(new IconRule() { ruleName = name, icon = null, enabled = true });
            Save();
        }

        public void RemoveRule(string ruleName_)
        {
            _rules.RemoveAll(x => x.ruleName == ruleName_);
            Save();
        }

        public void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
        #endregion
    }
}
