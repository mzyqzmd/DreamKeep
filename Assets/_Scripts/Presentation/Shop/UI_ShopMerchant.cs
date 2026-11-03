using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _DreamKeep.Presentation
{
    /// <summary>商人点击：进店时地毯是关着的，玩家点一下商人把货摊掀开（首次掀开后才亮出「继续」）。
    /// 悬停放大与头顶提示由同节点的 UI_HoverActive 负责，本类只上报点击。</summary>
    public sealed class UI_ShopMerchant : MonoBehaviour, IPointerClickHandler
    {
        /// <summary>点击上报：段入口据此开地毯。</summary>
        public event Action Clicked;

        // 商人本体是 SkeletonGraphic（raycastTarget 开着），点击即可落到这里
        public void OnPointerClick(PointerEventData _) => Clicked?.Invoke();
    }
}
