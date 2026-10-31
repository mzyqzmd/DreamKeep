using UnityEngine;

namespace _DreamKeep.EngineUtility
{
    /// <summary>面板（可开合容器）的抽象基类：封装 Show/Hide/Toggle 与可选置顶（SetAsLastSibling），
    /// 并把「显示/隐藏」钩子暴露为虚方法 OnShown/OnHidden 供子类填充刷新逻辑。具体面板继承本类。</summary>
    public abstract class UI_Panel : MonoBehaviour
    {
        #region 对外：开合
        public void Show()
        {
            gameObject.SetActive(true);
            OnShown();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            OnHidden();
        }

        public void Toggle()
        {
            if (gameObject.activeSelf) Hide();
            else Show();
        }

        #endregion

        protected virtual void OnShown() { }

        protected virtual void OnHidden() { }
    }
}
