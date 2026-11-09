using TMPro;
using UnityEngine;
using UnityEngine.UI;
using _DreamKeep.CommonPresentation;
using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.Presentation
{
    /// <summary>顶栏层信息位：层数数字 + 当前层图标随换层整体刷新，悬停图标弹出当前层说明气泡；
    /// 视觉与说明文案同地图节点共用 SO_MapNodeVisual（问号揭示后图标显变体、说明显揭示内容）。</summary>
    public sealed class UI_LayerBar : MonoBehaviour
    {
        [Tooltip("节点视觉配置（与地图节点共用：类型→图标/描边/揭示变体/悬停说明）")] [SerializeField]
        private SO_MapNodeVisual _visual;

        [Tooltip("悬停说明的气泡发起器：换层时写入当前层名称与描述")] [SerializeField]
        private UI_HoverTip _currentLayerHoverTip;

        [SerializeField] private Image _currentLayerIcon;
        [SerializeField] private TextMeshProUGUI _layerCount;

        #region 写当前层：层数文本、图标与悬停说明一次换装

        /// <summary>换层时整体刷新：文本写层数，图标铺当前节点的展示视觉（揭示变体优先），
        /// 说明按揭示内容的真实类型注入气泡（与图标变体语义一致）。</summary>
        public void SetFloor(int floor_, MapNode node_)
        {
            _layerCount.text = floor_.ToString();
            if (!_visual.TryGetDisplayVisual(node_.PointType, node_.RevealedType, out Sprite icon, out Sprite _))
            {
                Debug.LogError($"层栏视觉缺 {node_.PointType} 条目", this);
                return;
            }

            _currentLayerIcon.sprite = icon;
            if (_visual.TryGetInfo(node_.ResolvedType, out string tipName, out string tipDescription))
                _currentLayerHoverTip.SetContents(tipName, tipDescription);
            else
                Debug.LogError($"层栏说明缺 {node_.ResolvedType} 条目", this);
        }

        #endregion
    }
}