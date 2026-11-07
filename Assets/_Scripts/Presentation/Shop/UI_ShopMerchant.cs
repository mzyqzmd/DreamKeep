using System;
using Spine.Unity;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _DreamKeep.Presentation
{
    /// <summary>商人点击：进店时地毯是关着的，玩家点一下商人把货摊掀开（首次掀开后才亮出「继续」）。
    /// 悬停放大与头顶提示由同节点的 UI_HoverActive 负责，本类只上报点击。</summary>
    public sealed class UI_ShopMerchant : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private SkeletonGraphic _skeleton;
        private const string SkinIdle = "default";
        private const string SkinHover = "outline";

        /// <summary>点击上报：段入口据此开地毯。</summary>
        public event Action Clicked;

        // 商人本体是 SkeletonGraphic（raycastTarget 开着），点击即可落到这里
        public void OnPointerClick(PointerEventData _) => Clicked?.Invoke();

        public void OnPointerEnter(PointerEventData eventData) => ApplySkin(SkinHover);

        public void OnPointerExit(PointerEventData eventData) => ApplySkin(SkinIdle);

        private void ApplySkin(string skinName_)
        {
            if (_skeleton.Skeleton.Data.FindSkin(skinName_) == null) return;
            _skeleton.Skeleton.SetSkin(skinName_);
            _skeleton.Skeleton.SetSlotsToSetupPose();
        }
    }
}