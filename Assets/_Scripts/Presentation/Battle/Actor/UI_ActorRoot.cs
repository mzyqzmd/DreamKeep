using System.Collections.Generic;
using _DreamKeep.RuntimeData;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>战场单位区：持有纵深阵型五个槽位点（0 号为玩家、1~4 号为敌人，1 最靠前、4 最靠后），是单位出生摆位与
    /// 移动补间的唯一坐标来源；同时内嵌"生物 → 单位视图"登记表（单位装配时自登记、销毁时注销），供目标提示按生物反查
    /// 视图。指针悬停不在它这里裁决——由单位自己的 UI_ActorHoverBox 按指针事件接管。</summary>
    public sealed class UI_ActorRoot : MonoBehaviour
    {
        [SerializeField] private Transform _slot0;
        [SerializeField] private Transform _slot1;
        [SerializeField] private Transform _slot2;
        [SerializeField] private Transform _slot3;
        [SerializeField] private Transform _slot4;

        private readonly Dictionary<Creature, UI_Actor> _views = new();

        #region 按槽位号取槽位点：单位出生与移动都向它查询

        public Transform GetSlotTransform(int slot_) => slot_ switch
        {
            0 => _slot0,
            1 => _slot1,
            2 => _slot2,
            3 => _slot3,
            4 => _slot4,
            _ => null,
        };

        #endregion

        #region 对外暴露的登记与查询：单位视图装配时自登记、销毁时注销

        public void Register(Creature creature_, UI_Actor view_) => _views[creature_] = view_;

        public void Unregister(Creature creature_) => _views.Remove(creature_);

        public bool TryGetView(Creature creature_, out UI_Actor view_) => _views.TryGetValue(creature_, out view_);

        #endregion
    }
}