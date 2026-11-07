using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using _DreamKeep.ConfigData;

namespace _DreamKeep.Editor.Save
{
    /// <summary>存档配置目录烘焙器：手动执行菜单，扫描 Assets/_Data 的全部 ScriptableObject 资产，
    /// 把「资产 GUID ↔ 资产引用」写进 catalog；不在启动时隐式刷新，配置资产增删后需重跑一次。</summary>
    public static class SaveCatalogBaker
    {
        private const string CatalogPath = "Assets/Resources/SO_SaveCatalog.asset";
        private const string DataRoot = "Assets/_Data";

        #region 烘焙目录：菜单手动触发，catalog 缺失时先创建资产再写两条平行表

        [MenuItem("Tools/DreamKeep/Save/Bake Save Catalog")]
        public static void Bake()
        {
            SO_SaveCatalog catalog = AssetDatabase.LoadAssetAtPath<SO_SaveCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<SO_SaveCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            List<string> guids = new();
            List<ScriptableObject> assets = new();
            foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { DataRoot }))
            {
                ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset == null) continue;
                guids.Add(guid);
                assets.Add(asset);
            }

            SerializedObject serialized = new(catalog);
            WriteStringArray(serialized.FindProperty("_guids"), guids);
            WriteAssetArray(serialized.FindProperty("_assets"), assets);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[SaveCatalogBaker] 已烘焙 {guids.Count} 项配置资产 → {CatalogPath}");
        }

        #endregion

        private static void WriteStringArray(SerializedProperty property_, List<string> values_)
        {
            property_.arraySize = values_.Count;
            for (int i = 0; i < values_.Count; i++)
                property_.GetArrayElementAtIndex(i).stringValue = values_[i];
        }

        private static void WriteAssetArray(SerializedProperty property_, List<ScriptableObject> values_)
        {
            property_.arraySize = values_.Count;
            for (int i = 0; i < values_.Count; i++)
                property_.GetArrayElementAtIndex(i).objectReferenceValue = values_[i];
        }
    }
}
