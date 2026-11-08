#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace _DreamKeep.EngineUtility
{
    /// <summary>存档资产目录烘焙器：菜单手动执行，扫 Assets/_Data 全部 ScriptableObject 资产写进目录。
    /// 新增或移动配置资产后需重跑一次，否则该资产无法被存档换算成 GUID。</summary>
    public static class SaveCatalogBaker
    {
        private const string CatalogPath = "Assets/Resources/SO_SaveCatalog.asset";
        private const string DataRoot = "Assets/_Data";

        #region 烘焙目录：菜单手动触发，目录缺失时先创建资产再写两张平行表

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
            WriteStrings(serialized.FindProperty("_guids"), guids);
            WriteAssets(serialized.FindProperty("_assets"), assets);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[SaveCatalogBaker] 已烘焙 {guids.Count} 项资产 → {CatalogPath}（新增资产后需重跑）");
        }

        #endregion

        private static void WriteStrings(SerializedProperty property_, List<string> values_)
        {
            property_.arraySize = values_.Count;
            for (int i = 0; i < values_.Count; i++)
                property_.GetArrayElementAtIndex(i).stringValue = values_[i];
        }

        private static void WriteAssets(SerializedProperty property_, List<ScriptableObject> values_)
        {
            property_.arraySize = values_.Count;
            for (int i = 0; i < values_.Count; i++)
                property_.GetArrayElementAtIndex(i).objectReferenceValue = values_[i];
        }
    }
}
#endif
