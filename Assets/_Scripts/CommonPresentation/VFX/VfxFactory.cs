using _DreamKeep.EngineUtility;
using UnityEngine;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>通用特效工厂：挂在场景 GameRoot 下，持有受击/治疗两种一次性特效并按预制体分桶管理实例池——
    /// 父节点、世界坐标与缩放由调用方决定，本类不认识 Creature / UI_ActorRoot / 任何玩法事件；播完自动还池。</summary>
    public sealed class VfxFactory : MonoBehaviour
    {
        [Header("特效预制体")] [SerializeField] private UI_OneShotVfx _hitVfxPrefab;
        [SerializeField] private UI_OneShotVfx _healVfxPrefab;

        private MonoObjectPool<UI_OneShotVfx> _vfxPool;

        private void Awake() => _vfxPool = new MonoObjectPool<UI_OneShotVfx>(transform, onReturn_: ReturnToRoot);

        #region 对外播放：受击与治疗各一个固定入口

        /// <summary>受击特效：在指定父节点下、以给定世界坐标与缩放播放一次。</summary>
        public void PlayHit(Transform parent_, Vector3 worldPosition_, float scale_) =>
            Play(_hitVfxPrefab, parent_, worldPosition_, scale_);

        /// <summary>治疗特效：在指定父节点下、以给定世界坐标与缩放播放一次。</summary>
        public void PlayHeal(Transform parent_, Vector3 worldPosition_, float scale_) =>
            Play(_healVfxPrefab, parent_, worldPosition_, scale_);

        #endregion

        // 两种特效共用的成品段落：取池 → 挂到调用方给的父节点并置顶 → 写位姿 → 播完还池
        private void Play(UI_OneShotVfx prefab_, Transform parent_, Vector3 worldPosition_, float scale_)
        {
            UI_OneShotVfx vfx = _vfxPool.Get(prefab_);
            Transform vfxTransform = vfx.transform;
            vfxTransform.SetParent(parent_, false);
            vfxTransform.SetAsLastSibling();
            vfxTransform.localScale = Vector3.one * scale_;
            vfxTransform.position = worldPosition_;
            vfx.Play(() => _vfxPool.Release(vfx));
        }

        // 还池前归位到本组件名下：空闲实例不留在可能被销毁的单位下，下次取出再改挂
        private void ReturnToRoot(Component instance_) => instance_.transform.SetParent(transform, false);
    }
}