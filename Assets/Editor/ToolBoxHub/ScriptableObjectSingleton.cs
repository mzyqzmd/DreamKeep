using UnityEditor;
using UnityEngine;

namespace ToolBoxHub
{
    /// <summary>Editor 配置资产的懒加载单例基座：Load 无则 Create 落盘，收编逐类重复的 Instance 样板。
    /// 资产路径由子类以 AssetPath 指定（各工具目录自理）；加载后需要预填默认值的子类覆写 OnLoaded。</summary>
    public abstract class ScriptableObjectSingleton<T> : ScriptableObject where T : ScriptableObjectSingleton<T>
    {
        // CRTP：静态字段按泛型实参各一份，每个配置类型天然独立在用实例
        private static T _instance;

        /// <summary>子类资产路径（放在各自工具目录下）。</summary>
        protected abstract string AssetPath { get; }

        public static T Instance
        {
            get
            {
                if (_instance) return _instance;
                string path = AssetPathOf();
                _instance = AssetDatabase.LoadAssetAtPath<T>(path);
                if (!_instance)
                {
                    _instance = CreateInstance<T>();
                    AssetDatabase.CreateAsset(_instance, path);
                    AssetDatabase.SaveAssets();
                }

                _instance.OnLoaded();
                return _instance;
            }
        }

        /// <summary>实例加载后的钩子：需要预填默认值的子类覆写（如首次生成预置规则）。</summary>
        protected virtual void OnLoaded() { }

        // 抽象路径在静态上下文拿不到：用临时实例探一次（仅首次取 Instance 走到，即建即毁无副作用）
        private static string AssetPathOf()
        {
            T probe = CreateInstance<T>();
            string path = probe.AssetPath;
            DestroyImmediate(probe);
            return path;
        }
    }
}
