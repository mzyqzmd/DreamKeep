using System;
using System.IO;
using UnityEngine;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>存档物理层：按 slotId 拼 persistentDataPath 路径，同步读写一份 JSON。
    /// 写档先落 .tmp 再原子替换并留一份 .backup；主档读失败回落备份；只认识字符串，不认识任何 DTO。</summary>
    public sealed class SaveFileStore
    {
        private const string FileNameFormat = "current_run_{0}.json";
        private const string BackupSuffix = ".backup";
        private const string TempSuffix = ".tmp";

        private static string PathOf(int slotId_) =>
            Path.Combine(Application.persistentDataPath, string.Format(FileNameFormat, slotId_));

        #region 读写：主档失败回落备份，全坏返回 null 由服务层按无档处理

        public string Read(int slotId_)
        {
            string path = PathOf(slotId_);
            string json = TryRead(path);
            if (json != null) return json;
            string backup = TryRead(path + BackupSuffix);
            if (backup != null) Debug.LogWarning("[SaveFileStore] 主档读取失败，已回落到 .backup");
            return backup;
        }

        public void Write(int slotId_, string json_)
        {
            string path = PathOf(slotId_);
            string tmp = path + TempSuffix;
            try
            {
                File.WriteAllText(tmp, json_);
                if (File.Exists(path)) File.Replace(tmp, path, path + BackupSuffix);
                else File.Move(tmp, path);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveFileStore] 写档失败：{e.Message}");
                throw;
            }
        }

        #endregion

        #region 删除与清理：删档连备份与临时文件一起清，启动时清残留 .tmp

        public void Delete(int slotId_)
        {
            string path = PathOf(slotId_);
            RemoveIfExists(path);
            RemoveIfExists(path + BackupSuffix);
            RemoveIfExists(path + TempSuffix);
        }

        public void ClearTemporary(int slotId_) => RemoveIfExists(PathOf(slotId_) + TempSuffix);

        #endregion

        private static string TryRead(string path_)
        {
            if (!File.Exists(path_)) return null;
            try
            {
                return File.ReadAllText(path_);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveFileStore] 读取 {path_} 失败：{e.Message}");
                return null;
            }
        }

        private static void RemoveIfExists(string path_)
        {
            if (!File.Exists(path_)) return;
            try
            {
                File.Delete(path_);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveFileStore] 删除 {path_} 失败：{e.Message}");
            }
        }
    }
}
