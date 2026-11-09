using UnityEngine;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Presentation
{
    /// <summary>Boss 地图节点：SkeletonGraphic 骨架常播（预制体起始动画 animation 循环），明度走骨架 Graphic.color，不碰染色材质。
    /// 没有描边层：可被选中时把皮肤切到 stroke（描边皮肤）来展示轮廓，其余状态回 default。</summary>
    public sealed class UI_BossMapNode : UI_MapNode
    {
        private const string IdleSkin = "default";
        private const string OutlineSkin = "stroke";

        [Tooltip("Boss 骨架")] [SerializeField]
        private Spine.Unity.SkeletonGraphic _skeleton;

        // 皮肤只登记附件，换完必须重新套用到插槽才生效
        protected override void OnStateChanged(EMapNodeState state_, bool travelable_, bool ancient_)
        {
            if (_skeleton == null || !_skeleton.IsValid) return;
            _skeleton.Skeleton.SetSkin(travelable_ ? OutlineSkin : IdleSkin);
            _skeleton.Skeleton.SetSlotsToSetupPose();
        }

        protected override void WriteDim(float alpha_)
        {
            Color color = _skeleton.color;
            color.a = alpha_;
            _skeleton.color = color;
        }
    }
}
