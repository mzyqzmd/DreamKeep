using System.Collections.Generic;
using _DreamKeep.RuntimeData;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>战场单位区：持有纵深阵型四个槽位点（0 号为玩家、1~3 号为敌人，1 最靠前、3 最靠后），是单位出生摆位与
    /// 移动补间的唯一坐标来源；同时内嵌"生物 → 单位视图"登记表（单位装配时自登记、销毁时注销），供目标提示按生物反查
    /// 视图，并对外提供瞄准期间的悬停压制入口。指针悬停不在它这里裁决——由单位自己的 UI_ActorHoverBox 按指针事件接管。</summary>
    public sealed class UI_ActorRoot : MonoBehaviour
    {
        [SerializeField] private Transform _slot0;
        [SerializeField] private Transform _slot1;
        [SerializeField] private Transform _slot2;
        [SerializeField] private Transform _slot3;

        private readonly Dictionary<Creature, UI_Actor> _views = new();

        #region 按槽位号取槽位点：单位出生与移动都向它查询

        public Transform GetSlotTransform(int slot_) => slot_ switch
        {
            0 => _slot0,
            1 => _slot1,
            2 => _slot2,
            3 => _slot3,
            _ => null,
        };

        #endregion

        #region 对外暴露的登记与查询：单位视图装配时自登记、销毁时注销

        public void Register(Creature creature_, UI_Actor view_) => _views[creature_] = view_;

        public void Unregister(Creature creature_) => _views.Remove(creature_);

        public bool TryGetView(Creature creature_, out UI_Actor view_) => _views.TryGetValue(creature_, out view_);

        #endregion

        #region 瞄准期间的悬停压制：由目标提示按"这张牌会命中的目标"逐帧对账

        /// <summary>压制悬停：传 null 解除全部；传列表则保留列表内生物的悬停、压制其余（空列表 = 全部压制）。
        /// 视图随死亡自毁时登记表可能残留伪空引用，逐个跳过。</summary>
        public void ApplyHoverSuppression(IReadOnlyList<Creature> keep_)
        {
            foreach (var entry in _views)
            {
                UI_ActorHoverBox box = entry.Value ? entry.Value.HoverBox : null;
                if (!box) continue;
                bool keep = keep_ == null || Contains(keep_, entry.Key); // 没给名单就是每个单位都不例外 → 全部放开
                box.SetHoverSuppressed(!keep);
            }
        }

        // 目标查找：单位不过两三个，直接线性比较，省掉一份每帧临时集合
        private static bool Contains(IReadOnlyList<Creature> list_, Creature creature_)
        {
            for (int i = 0; i < list_.Count; i++)
                if (list_[i] == creature_) return true;
            return false;
        }

        #endregion
    }
}