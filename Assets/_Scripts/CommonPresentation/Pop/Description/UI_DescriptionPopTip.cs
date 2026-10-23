using TMPro;
using UnityEngine;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>说明气泡：标题与描述的复用弹板，内容由发起器（UI_HoverTip）在弹出时写入；
    /// 标题为空自动收起标题行（意图类说明只有描述），实例取还归 PopRoot、显隐与定位归发起器。
    /// 自带 overrideSorting 的 Canvas：挂到哪个父级就自动压在那层 Canvas 的其余内容之上，层级由父级驱动、挂载方不需要知道 Canvas。</summary>
    public sealed class UI_DescriptionPopTip : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _name;
        [SerializeField] private TextMeshProUGUI _description;
        [SerializeField] private Canvas _canvas;

        // 父级一变就对齐绘制层级：挂载方只管 SetParent，压不压得住兄弟节点由气泡自己负责
        private void OnTransformParentChanged()
        {
            if (!transform.parent) return;
            Canvas parentCanvas = transform.parent.GetComponentInParent<Canvas>();
            if (!parentCanvas) return;
            _canvas.sortingLayerID = parentCanvas.sortingLayerID;
            _canvas.sortingOrder = parentCanvas.sortingOrder + 1;
        }

        public void SetContent(string name_, string description_)
        {
            bool hasName = !string.IsNullOrEmpty(name_);
            _name.gameObject.SetActive(hasName);
            if (hasName) _name.text = name_;
            _description.text = description_;
        }
    }
}
