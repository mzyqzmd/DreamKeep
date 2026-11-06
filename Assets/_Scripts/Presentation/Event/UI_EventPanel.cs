using TMPro;
using UnityEngine;
using VContainer;
using _DreamKeep.ConfigData;
using _DreamKeep.GameRun;

namespace _DreamKeep.Presentation
{
    /// <summary>事件面板：订阅事件段事实写事件名、背景与"叙事/结果"共用的整段文本位——
    /// 第 1 页写配置叙事，第 2 页按所选选项换结果文案；选项与「继续」按钮由选项组承载。</summary>
    public sealed class UI_EventPanel : MonoBehaviour
    {
        [Tooltip("事件名标题文本")] [SerializeField] private TMP_Text _title;

        [Tooltip("第 1 页叙事 / 第 2 页结果共用的整段文本")] [SerializeField] private TMP_Text _description;

        [Tooltip("事件背景整图（按配置逐层替换）")] [SerializeField] private UI_BackgroundView _background;

        [Inject]
        public void Construct(EventEventDispatcher events_)
        {
            events_.Register<EE_EventOpened>(opened_ => Bind(opened_.Config));
            events_.Register<EE_ResultShown>(shown_ => ShowResult(shown_.ResultText));
        }

        private void Bind(SO_Event config_)
        {
            _title.text = config_.EventName;
            _description.text = config_.Narrative;
            if (_background) _background.Apply(config_.Background);
        }

        private void ShowResult(string resultText_) => _description.text = resultText_;
    }
}
