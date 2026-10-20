using System;
using _DreamKeep.EngineUtility;
using UnityEngine;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>通用特效工厂：挂在场景 GameRoot 下，持有常用特效预制体并按预制体分桶管理实例池——
    /// 受击/治疗走固定入口（PlayHit / PlayHeal），自定义特效仍可走通用入口（Play）；
    /// 父节点、世界坐标与缩放由调用方决定，本类不认识 Creature / UI_ActorRoot / 任何玩法事件；播完还池并回调。</summary>
    public sealed class VfxFactory : MonoBehaviour
    {
        [Header("特效预制体")] [SerializeField] private UI_OneShotVfx _hitVfxPrefab;
        [SerializeField] private UI_OneShotVfx _healVfxPrefab;

        private MonoObjectPool<UI_OneShotVfx> _vfxPool;

        private void Awake()
        {
            _vfxPool = new MonoObjectPool<UI_OneShotVfx>(transform, onReturn_: ReturnToRoot);
        }

        #region 对外播放：常用特效走固定入口，自定义特效走通用入口

        /// <summary>受击特效：在指定父节点下、以给定世界坐标与缩放播放一次。</summary>
        public void PlayHit(Transform parent_, Vector3 worldPosition_, float scale_, Action onFinished_ = null) =>
            Play(_hitVfxPrefab, parent_, worldPosition_, scale_, onFinished_);

        /// <summary>治疗特效：在指定父节点下、以给定世界坐标与缩放播放一次。</summary>
        public void PlayHeal(Transform parent_, Vector3 worldPosition_, float scale_, Action onFinished_ = null) =>
            Play(_healVfxPrefab, parent_, worldPosition_, scale_, onFinished_);

        /// <summary>通用入口：任意一次性特效预制体，摆位与缩放由调用方决定；播完还池并回调（可为空）。</summary>
        private void Play(UI_OneShotVfx prefab_, Transform parent_, Vector3 worldPosition_, float scale_,
            Action onFinished_ = null)
        {
            UI_OneShotVfx vfx = _vfxPool.Get(prefab_);
            Transform vfxTransform = vfx.transform;
            vfxTransform.SetParent(parent_, false);
            vfxTransform.SetAsLastSibling();
            vfxTransform.localScale = Vector3.one * scale_;
            vfxTransform.position = worldPosition_;
            vfx.Play(() =>
            {
                _vfxPool.Release(vfx);
                onFinished_?.Invoke();
            });
        }

        #endregion

        // 还池前归位到本组件名下：空闲实例不留在可能被销毁的单位下，下次取出再改挂
        private void ReturnToRoot(Component instance_) => instance_.transform.SetParent(transform, false);
    }
}