using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>地图面板（占位）：跑图系统落地前只负责开合，内容留空。
    /// 面板自己不决定何时出现——顶栏按钮与战后流程都调同一组开合入口，重复调用无副作用。</summary>
    public class UI_MapPanel : MonoBehaviour
    {
        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        public void Toggle() => gameObject.SetActive(!gameObject.activeSelf);
    }
}
