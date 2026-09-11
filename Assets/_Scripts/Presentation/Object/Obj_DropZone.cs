using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>敌人落点：占据一个阵型槽位，作为拖拽的命中区域与高亮载体。</summary>
    public class Obj_DropZone : MonoBehaviour
    {
        [SerializeField] private int _slot;
        [SerializeField] private SpriteRenderer _frame;
        [SerializeField] private Color _highlightColor = Color.yellow;

        private Collider2D _zone;
        private Color _normalColor;

        public int Slot => _slot;

        private void Awake()
        {
            _zone = GetComponent<Collider2D>();
            _normalColor = _frame.color;
        }

        #region 判定一个世界坐标是否落在本落点内
        public bool Contains(Vector3 worldPosition_) => _zone.OverlapPoint(worldPosition_);
        #endregion

        #region 切换高亮
        public void SetHighlight(bool highlighted_) => _frame.color = highlighted_ ? _highlightColor : _normalColor;
        #endregion
    }
}
