using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>地图连线上的椭圆点：每次激活时在 ±_tiltRange 内随机倾斜，营造手绘链条感；纯展示，不接指针事件。</summary>
    public class UI_MapDot : MonoBehaviour
    {
        [Tooltip("随机倾斜幅度上限（度）")] [SerializeField]
        private float _tiltRange = 10f;

        private void OnEnable() =>
            transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-_tiltRange, _tiltRange));
    }
}