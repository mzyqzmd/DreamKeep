using TMPro;
using UnityEngine;
using _DreamKeep.ConfigData;

namespace _DreamKeep.Presentation
{
    /// <summary>事件面板：展示事件名、背景整图与"叙事/结果"共用的整段文本位——
    /// 第 1 页写配置叙事，第 2 页按所选选项换结果文案；选项与「继续」按钮由选项组承载。</summary>
    public sealed class UI_EventPanel : MonoBehaviour
    {
        [Tooltip("事件名标题文本")] [SerializeField] private TMP_Text _title;

        [Tooltip("第 1 页叙事 / 第 2 页结果共用的整段文本")] [SerializeField] private TMP_Text _description;

        [Tooltip("事件背景整图（按配置逐层替换）")] [SerializeField] private UI_BackgroundView _background;

        /// <summary>绑定事件：写事件名、铺背景、把叙事落到第 1 页。</summary>
        public void Bind(SO_Event config_)
        {
            _title.text = config_.EventName;
            _description.text = config_.Narrative;
            if (_background) _background.Apply(config_.Background);
        }

        /// <summary>切到第 2 页：只换结果文案，标题与背景保持不动。</summary>
        public void ShowResult(string resultText_) => _description.text = resultText_;
    }
}
