using Cysharp.Threading.Tasks;
using Spine;
using Spine.Unity;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _DreamKeep.Presentation
{
    /// <summary>宝箱视图：悬停换 act1_stroke 皮肤、移开换回 act1；点击开箱（保持 act1 并播 animation），
    /// 播到 50% 放行让段入口出遗物；领取后播 shine_fade 并锁定交互。指针事件由本节点统一承接。</summary>
    public sealed class UI_Chest : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private const string
            SkinIdle = "act1",
            SkinHover = "act1_stroke",
            AnimOpen = "animation",
            AnimShine = "shine_fade";

        [SerializeField] private SkeletonGraphic _skeleton;
        [Tooltip("开箱动画播到该比例（0~1）时放行出遗物")] [SerializeField]
        private float _openReleaseRatio = 0.5f;

        private UniTaskCompletionSource _openedSource;
        private bool _opened; // 已开箱：锁定悬停换肤，等待闪光
        private bool _locked; // 已领取：彻底锁死交互
        private bool _frozen; // 进场冻结只做一次的标记

        private void Awake()
        {
            if (!_skeleton) _skeleton = GetComponentInChildren<SkeletonGraphic>(true);
            ApplySkin(SkinIdle);
        }

        private void LateUpdate()
        {
            if (_frozen) return;
            _frozen = true;
            // 场景 startingAnimation=animation 让 SkeletonGraphic 在 Awake 后自行开播，实测其时机
            // 晚于 Start，会覆盖 Start 里的 ClearTracks，故等首帧 LateUpdate（必然晚于它）再钉帧。
            PinToFirstFrame();
        }

        public void OnPointerEnter(PointerEventData eventData_)
        {
            if (_opened || _locked) return;
            ApplySkin(SkinHover);
            PinToFirstFrame();
        }

        public void OnPointerExit(PointerEventData eventData_)
        {
            if (_opened || _locked) return;
            ApplySkin(SkinIdle);
            PinToFirstFrame();
        }

        public void OnPointerClick(PointerEventData eventData_)
        {
            if (_opened || _locked) return;
            OpenAsync().Forget();
        }

        // 钉在开箱动画第一帧：setup pose 是敞开的怪异形态，闭合外观只存在于 animation。进场与悬停
        // 换肤（ApplySkin 内含 SetSlotsToSetupPose）都必须跟这一步。姿态真正落到骨架在 Apply（每帧
        // Update→ApplyAnimation），state.Update 只推时间；轨道清空后每帧 Apply 变空操作，故清轨道前
        // 手动 ApplyAnimation 一次（含世界变换更新），否则骨架会停在换肤打回的敞开 setup pose。
        // 也不可用 freeze/timeScale——freeze 停掉 UpdateMesh 悬停换皮不渲染，timeScale=0 每帧 Apply
        // 把换上的附件覆盖回去；清轨道后姿态钉死、UpdateMesh 照常，附件按名字在当前皮肤下解析，
        // 描边/普通皮都不丢，点击时 SetAnimation 从同一帧重播无跳变。
        private void PinToFirstFrame()
        {
            Spine.AnimationState state = _skeleton.AnimationState;
            state.SetAnimation(0, AnimOpen, false);
            state.Update(0f);
            _skeleton.ApplyAnimation();
            state.ClearTracks();
        }

        #region 对外：开箱等待与闪光锁定

        /// <summary>等待玩家开箱：点击即播 animation，播到 _openReleaseRatio 比例时放行（遗物在此刻生成）。</summary>
        public UniTask WaitOpenedAsync()
        {
            _openedSource ??= new UniTaskCompletionSource();
            return _openedSource.Task;
        }

        /// <summary>播放领取闪光动画并锁定交互：领取成功后由段入口调用。</summary>
        public async UniTask PlayShineFadeAndLockAsync()
        {
            _locked = true;
            TrackEntry entry = _skeleton.AnimationState.SetAnimation(0, AnimShine, false);
            if (entry == null || entry.Animation == null) return;
            float duration = entry.Animation.Duration;
            await UniTask.WaitUntil(() => entry.TrackTime >= duration, cancellationToken: destroyCancellationToken);
        }

        #endregion

        private async UniTask OpenAsync()
        {
            _opened = true;
            ApplySkin(SkinIdle); // 开箱保持 act1，覆盖可能残留的悬停换肤
            TrackEntry entry = _skeleton.AnimationState.SetAnimation(0, AnimOpen, false);
            if (entry == null || entry.Animation == null)
            {
                _openedSource?.TrySetResult();
                return;
            }

            float threshold = entry.Animation.Duration * _openReleaseRatio;
            await UniTask.WaitUntil(() => entry.TrackTime >= threshold, cancellationToken: destroyCancellationToken);
            _openedSource?.TrySetResult();
        }

        private void ApplySkin(string skinName_)
        {
            if (_skeleton == null || _skeleton.Skeleton == null) return;
            if (_skeleton.Skeleton.Data.FindSkin(skinName_) == null) return;
            _skeleton.Skeleton.SetSkin(skinName_);
            _skeleton.Skeleton.SetSlotsToSetupPose();
        }
    }
}
