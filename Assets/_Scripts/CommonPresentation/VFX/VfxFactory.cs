using System;
using _DreamKeep.EngineUtility;
using _DreamKeep.RuntimeData;
using UnityEngine;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>通用特效工厂：挂在场景 GameRoot 下，按预制体分桶持有一批一次性特效并管理实例池——
    /// 父节点、世界坐标与缩放由调用方决定，本类不认识 Creature / UI_ActorRoot / 任何玩法事件；播完自动还池。
    /// 每个槽位都可以留空：没接预制体的特效类型直接不播（不取池、不报错），素材做好后接上即生效。</summary>
    public sealed class VfxFactory : MonoBehaviour
    {
        [Header("特效预制体")] [SerializeField] private UI_OneShotVfx _hitVfxPrefab;
        [SerializeField] private UI_OneShotVfx _healVfxPrefab;
        [SerializeField] private UI_OneShotVfx _blockVfxPrefab;
        [SerializeField] private UI_PerkAppliedVfx _perkVfxPrefab;
        [SerializeField] private UI_PotionSplashVfx _potionSplashVfxPrefab;
        [SerializeField] private UI_OneShotVfx _restSmokeVfxPrefab;
        [SerializeField] private UI_OneShotVfx _smithForgeVfxPrefab;

        private MonoObjectPool<UI_OneShotVfx> _vfxPool;

        private void Awake() => _vfxPool = new MonoObjectPool<UI_OneShotVfx>(transform, onReturn_: ReturnToRoot);

        #region 对外播放：受击 / 治疗 / 格挡 / 施加状态 / 药水落地各一个固定入口

        /// <summary>受击特效：在指定父节点下、以给定世界坐标与缩放播放一次。</summary>
        public void PlayHit(Transform parent_, Vector3 worldPosition_, float scale_) =>
            Play(_hitVfxPrefab, parent_, worldPosition_, scale_);

        /// <summary>治疗特效：在指定父节点下、以给定世界坐标与缩放播放一次。</summary>
        public void PlayHeal(Transform parent_, Vector3 worldPosition_, float scale_) =>
            Play(_healVfxPrefab, parent_, worldPosition_, scale_);

        /// <summary>格挡特效：在指定父节点下、以给定世界坐标与缩放播放一次。</summary>
        public void PlayBlock(Transform parent_, Vector3 worldPosition_, float scale_) =>
            Play(_blockVfxPrefab, parent_, worldPosition_, scale_);

        /// <summary>施加状态特效：在指定父节点下、以给定世界坐标与缩放播放一次，内容取自状态运行时实例。</summary>
        public void PlayPerk(Transform parent_, Vector3 worldPosition_, float scale_, BattlePerk perk_)
        {
            if (!_perkVfxPrefab) return;
            UI_OneShotVfx vfx = Rent(_perkVfxPrefab, parent_, worldPosition_, scale_);
            if (vfx is UI_PerkAppliedVfx perkVfx) perkVfx.Bind(perk_);
            vfx.Play(() => _vfxPool.Release(vfx));
        }

        /// <summary>药水落地特效：在指定父节点下、以给定世界坐标与缩放播放一次，液体色取自那瓶药水。</summary>
        public void PlayPotionSplash(Transform parent_, Vector3 worldPosition_, float scale_, Color color_)
        {
            if (!_potionSplashVfxPrefab) return;
            UI_OneShotVfx vfx = Rent(_potionSplashVfxPrefab, parent_, worldPosition_, scale_);
            if (vfx is UI_PotionSplashVfx splashVfx) splashVfx.Bind(color_);
            vfx.Play(() => _vfxPool.Release(vfx));
        }

        /// <summary>休息烟雾：播放时长由预制体上的 UI_RestSmokeVfx 决定；槽位空则立刻回调（等待方不会挂死）。</summary>
        public void PlayRestSmoke(Transform parent_, Vector3 worldPosition_, float scale_, Action onFinished_) =>
            PlayWithCallback(_restSmokeVfxPrefab, parent_, worldPosition_, scale_, onFinished_);

        /// <summary>锻造锤击：三段循环由预制体上的 UI_SmithForgeVfx 驱动；槽位空则立刻回调。</summary>
        public void PlaySmithForge(Transform parent_, Vector3 worldPosition_, float scale_, Action onFinished_) =>
            PlayWithCallback(_smithForgeVfxPrefab, parent_, worldPosition_, scale_, onFinished_);

        #endregion

        // 无参数特效的成品段落：取池摆位后直接开播；没接预制体就什么都不做
        private void Play(UI_OneShotVfx prefab_, Transform parent_, Vector3 worldPosition_, float scale_)
        {
            if (!prefab_) return;
            UI_OneShotVfx vfx = Rent(prefab_, parent_, worldPosition_, scale_);
            vfx.Play(() => _vfxPool.Release(vfx));
        }

        // 带完成回调的成品段落：还池之后回调，供段内按"播完再做下一步"编排
        private void PlayWithCallback(UI_OneShotVfx prefab_, Transform parent_, Vector3 worldPosition_, float scale_,
            Action onFinished_)
        {
            if (!prefab_)
            {
                onFinished_?.Invoke();
                return;
            }

            UI_OneShotVfx vfx = Rent(prefab_, parent_, worldPosition_, scale_);
            vfx.Play(() =>
            {
                _vfxPool.Release(vfx);
                onFinished_?.Invoke();
            });
        }

        // 取池与摆位：挂到调用方给的父节点并置顶 → 写缩放与世界坐标。内容绑定由各通道自己补
        private UI_OneShotVfx Rent(UI_OneShotVfx prefab_, Transform parent_, Vector3 worldPosition_, float scale_)
        {
            UI_OneShotVfx vfx = _vfxPool.Get(prefab_);
            Transform vfxTransform = vfx.transform;
            vfxTransform.SetParent(parent_, false);
            vfxTransform.SetAsLastSibling();
            vfxTransform.localScale = Vector3.one * scale_;
            vfxTransform.position = worldPosition_;
            return vfx;
        }

        // 还池前归位到本组件名下：空闲实例不留在可能被销毁的单位下，下次取出再改挂
        private void ReturnToRoot(Component instance_) => instance_.transform.SetParent(transform, false);
    }
}
