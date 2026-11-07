using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using _DreamKeep.ConfigData;
using _DreamKeep.GameRun;

namespace _DreamKeep.Presentation
{
    /// <summary>存档门面：把整局状态拍成 DTO 落盘、按版本号读回，并持有配置目录供 GUID 反查。
    /// 由 RunLifetimeScope 注册为容器实例，业务侧不碰文件与 JSON；单档位只在 slotId 上预留参数。</summary>
    public sealed class SaveService
    {
        public const int CurrentSchemaVersion = 1;

        private const string CatalogResource = "SO_SaveCatalog";

        private readonly SaveFileStore _store = new();
        private readonly SO_SaveCatalog _catalog;

        public SaveService()
        {
            _catalog = Resources.Load<SO_SaveCatalog>(CatalogResource);
            if (_catalog == null)
                Debug.LogError("[SaveService] 找不到 Resources/SO_SaveCatalog，请先执行 DreamKeep/Save/Bake Save Catalog");
            _store.ClearTemporary(0);
        }

        #region 查询与读取：版本不符或主档备份全坏一律按无档处理并提示

        public bool HasSave(int slotId_ = 0) => Load(slotId_) != null;

        public SaveData Load(int slotId_ = 0)
        {
            string json = _store.Read(slotId_);
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("[SaveService] 主档与备份都不可用，按无档处理");
                return null;
            }

            try
            {
                SaveData data = JsonConvert.DeserializeObject<SaveData>(json);
                if (data == null || data.SchemaVersion != CurrentSchemaVersion)
                {
                    Debug.LogWarning($"[SaveService] 存档版本 {data?.SchemaVersion} 与当前 {CurrentSchemaVersion} 不符，按无档处理");
                    return null;
                }

                return data;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveService] 存档解析失败，按无档处理：{e.Message}");
                return null;
            }
        }

        #endregion

        #region 写档：段上下文与待领奖励都由调用方给定，配置项经 catalog 换算成 GUID

        public void Save(RunState run_, ESaveSegmentContext context_, uint segmentEntryRngState_,
            IReadOnlyList<RunReward> rewards_ = null, int slotId_ = 0)
        {
            if (_catalog == null) return;
            SaveData data = new()
            {
                SchemaVersion = CurrentSchemaVersion,
                SegmentContext = context_,
                SaveTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                SegmentEntryRngState = segmentEntryRngState_,
                Run = RunStateSave.ToSave(run_, _catalog, Warn),
                PendingRewards = rewards_ == null
                    ? new List<SaveReward>()
                    : RunStateSave.ToRewardSave(rewards_, _catalog, Warn),
            };
            _store.Write(slotId_, JsonConvert.SerializeObject(data, Formatting.Indented));
        }

        #endregion

        #region 还原：把磁盘 DTO 重建回运行时对象，缺配置只跳过该条并告警

        public RunState RestoreRun(SaveData data_) =>
            data_?.Run == null || _catalog == null ? null : RunStateSave.FromSave(data_.Run, _catalog, Warn);

        public List<RunReward> RestoreRewards(SaveData data_) =>
            _catalog == null ? new List<RunReward>() : RunStateSave.FromRewardSave(data_?.PendingRewards, _catalog, Warn);

        #endregion

        public void Delete(int slotId_ = 0) => _store.Delete(slotId_);

        #region 三个安全点：段入口记下随机流位置，奖励结算沿用同一位置，收段清掉段上下文

        public void RestoreSegmentEntryState(uint state_) => _segmentEntryRngState = state_;

        public void SaveEntering(RunState run_, int slotId_ = 0)
        {
            _segmentEntryRngState = run_.Rng.State;
            Save(run_, ESaveSegmentContext.Entering, _segmentEntryRngState, null, slotId_);
        }

        public void SaveAwaitingReward(RunState run_, IReadOnlyList<RunReward> rewards_, int slotId_ = 0) =>
            Save(run_, ESaveSegmentContext.AwaitingReward, _segmentEntryRngState, rewards_, slotId_);

        public void SaveOnMap(RunState run_, int slotId_ = 0) =>
            Save(run_, ESaveSegmentContext.None, 0, null, slotId_);

        #endregion

        private uint _segmentEntryRngState;

        private static void Warn(string message_) => Debug.LogWarning(message_);
    }
}
