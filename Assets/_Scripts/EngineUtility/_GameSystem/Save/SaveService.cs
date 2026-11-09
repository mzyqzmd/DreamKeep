using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace _DreamKeep.EngineUtility
{
    /// <summary>本地单档静态门面：只认识 ISaveable 与读写袋，不认识 VContainer 与任何游戏类型。
    /// 写盘遍历绑定进来的集合逐个 Capture，按实现类类型全名聚合进单文件；读盘先把整份快照静态暂存，
    /// 跨过场景加载与容器装配后再统一 Restore 分发。静态字段在域重载与 Play 启动时重置，不缓存跨场景对象。
    /// 为什么必须是静态：消费方横跨三层生命周期——常驻编排者在整局 scope 建起来之前就要读写档（主菜单的继续/删档），
    /// 组合根在装配期 Bind 与 ApplyRestore，段组合根按 RunState 的待领奖励清单判定领奖恢复；做成实例就得挂 App 级常驻 scope 再往下传两层，
    /// 反而比静态更绕。域重载后的残留由 ResetStatics 兜住。</summary>
    public static class SaveService
    {
        private const string FileName = "DreamSpireGameSave.json";

        private static SaveFile _pending;
        private static IReadOnlyList<ISaveable> _saveables;
        private static SO_SaveCatalog _catalog;
        private static int _restoreSegmentKind = -1;

        /// <summary>磁盘上是否存在存档文件。</summary>
        public static bool HasSave => File.Exists(FilePath);

        /// <summary>是否已载入一份待复原快照：组合根据此选择读档构造分支。</summary>
        public static bool HasPendingRestore => _pending != null;

        /// <summary>资产 GUID 目录：懒加载一次并复用，域重载后自动重新取，业务侧换算资产不必各自注入。</summary>
        public static SO_SaveCatalog Catalog => _catalog ??= Resources.Load<SO_SaveCatalog>("SO_SaveCatalog");

        private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _pending = null;
            _saveables = null;
            _catalog = null;
            ClearRestoreIntent();
        }

        #region 读档意图：段编排者 Restore 时写入，编排层消费一次后清空

        public static void MarkRestoreIntent(int segmentKind_) => _restoreSegmentKind = segmentKind_;

        public static void ClearRestoreIntent()
        {
            _pending = null;
            _restoreSegmentKind = -1;
        }

        /// <summary>常驻编排者取走首段种类并清该字段，读档只分派一次。</summary>
        public static int ConsumeRestoreSegmentKind()
        {
            int kind = _restoreSegmentKind;
            _restoreSegmentKind = -1;
            return kind;
        }

        #endregion

        #region 绑定：组合根在容器装配完成后交接集合，卸载整局场景时解绑

        public static void Bind(IReadOnlyList<ISaveable> saveables_) => _saveables = saveables_;

        public static void Unbind() => _saveables = null;

        #endregion

        #region 读盘：解析失败当坏档删掉并切回无档态，解析成功只暂存快照不做分发

        public static bool Load()
        {
            _pending = null;
            if (!HasSave) return false;
            try
            {
                SaveFile file = JsonConvert.DeserializeObject<SaveFile>(File.ReadAllText(FilePath));
                if (file == null) return false;
                _pending = file;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveService] 存档解析失败，按无档处理：{e.Message}");
                Delete();
                return false;
            }
        }

        #endregion

        #region 写盘：聚合各实现类的袋，同步一次写单文件

        public static void Save()
        {
            if (_saveables == null || _saveables.Count == 0)
            {
                Debug.LogError("[SaveService] 未绑定存档对象集合，本次写盘跳过");
                return;
            }

            SaveFile file = new() { Entries = new Dictionary<string, JObject>() };
            for (int i = 0; i < _saveables.Count; i++)
            {
                ISaveable saveable = _saveables[i];
                if (saveable == null) continue;
                SaveWriter writer = new();
                saveable.Capture(writer);
                string key = saveable.GetType().FullName;
                if (file.Entries.ContainsKey(key))
                {
                    Debug.LogError($"[SaveService] 存档键重复：{key}，后者已跳过");
                    continue;
                }

                file.Entries.Add(key, writer.Bag);
            }

            try { File.WriteAllText(FilePath, JsonConvert.SerializeObject(file, Formatting.Indented)); }
            catch (Exception e) { Debug.LogError($"[SaveService] 写盘失败：{e.Message}"); }
        }

        #endregion

        #region 删档：清文件与待读快照，主菜单据此切回无档态

        public static void Delete()
        {
            _pending = null;
            try
            {
                if (File.Exists(FilePath)) File.Delete(FilePath);
            }
            catch (Exception e) { Debug.LogError($"[SaveService] 删档失败：{e.Message}"); }
        }

        #endregion

        #region 复原：统一分发各袋，缺袋给空袋让业务侧走缺省值；消费完即清快照

        public static void ApplyRestore()
        {
            if (_pending == null || _saveables == null)
            {
                _pending = null;
                return;
            }

            for (int i = 0; i < _saveables.Count; i++)
            {
                ISaveable saveable = _saveables[i];
                if (saveable == null) continue;
                _pending.Entries.TryGetValue(saveable.GetType().FullName, out JObject bag);
                saveable.Restore(new SaveReader(bag));
            }

            _pending = null;
        }

        #endregion

        #region 待读快照按类型取袋：读档构造分支在容器 Build 之前用它重建整局状态

        public static ISaveReader PendingReaderOf(string key_)
        {
            if (_pending == null) return null;
            _pending.Entries.TryGetValue(key_, out JObject bag);
            return new SaveReader(bag);
        }

        #endregion
    }
}
