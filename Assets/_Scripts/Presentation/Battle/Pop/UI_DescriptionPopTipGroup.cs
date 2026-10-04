using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace _DreamKeep.Presentation
{
    /// <summary>描述气泡容器：挂在 Canvas_Pop/PopTextRoot 下，统一承接"一组描述气泡"的弹出——
    /// 条目按序自下而上堆叠在锚点上方，将顶出屏幕上沿时换列（列内间距/列间距/屏幕边距/换列方向 Inspector 可调）；
    /// 气泡实例一次生成永久复用（池化），运行期不订阅任何域事件，只被 UI_PerkGroup 与 UI_EnemyIntentGroup 经 PopTextRoot 领用。</summary>
    public class UI_DescriptionPopTipGroup : MonoBehaviour
    {
        [Header("气泡")] [SerializeField] private UI_DescriptionPopTip _tipPrefab;

        private readonly List<UI_DescriptionPopTip> _tipPool = new();
        private readonly List<(string Name, string Description)> _items = new();
        private object _requester;

        #region 对外展示：一组条目一次弹出、超屏换列、按发起者收起

        /// <summary>弹出一组描述气泡：条目按列表顺序自下而上堆叠在锚点上方，顶出屏幕上沿的换列排布。</summary>
        public void Show(object requester_, IReadOnlyList<(string Name, string Description)> items_, Vector3 worldAnchor_)
        {
            if (items_ == null || items_.Count == 0)
            {
                Hide(requester_);
                return;
            }

            _requester = requester_;
            _items.Clear();
            _items.AddRange(items_);
        }

        /// <summary>原位改写某条气泡内容（层数衰减等实时刷新），高度变化后整组重排。</summary>
        public void SetItem(int index_, string name_, string description_)
        {
            if (_requester == null || index_ < 0 || index_ >= _items.Count) return;
            _items[index_] = (name_, description_);
        }

        /// <summary>收起：只认当前发起者，防别的组 OnDisable 误收正在展示的气泡。</summary>
        public void Hide(object requester_)
        {
            if (_requester != requester_) return;
            _requester = null;
            _items.Clear();
            foreach (UI_DescriptionPopTip tip in _tipPool) tip.gameObject.SetActive(false);
        }

        #endregion
    }
}
