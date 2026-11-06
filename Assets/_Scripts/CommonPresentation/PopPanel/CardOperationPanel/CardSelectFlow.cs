using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using _DreamKeep.RuntimeData;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>卡牌选板取用-等待-提交流：四段入口共用的「Show → （可选关取消）→ await 结果 → 回调提交」一步式编排。
    /// 面板自管选牌与演出；关闭键交还 null = 取消，由调用方原样交回域流程。</summary>
    public static class CardSelectFlow
    {
        /// <summary>打开选板并等玩家结果后回调提交。cancelable=false 用于强制选牌：
        /// 必须先 Show 再关取消许可——Show 内部的清场会把取消许可复位。</summary>
        public static async UniTask OpenAndSubmitAsync(UI_CardSelectPanel panel_, Player player_, int count_,
            Action<IReadOnlyList<CardModel>> submit_, bool cancelable_ = true)
        {
            panel_.Show(player_, count_);
            if (!cancelable_) panel_.SetCancelable(false);
            submit_(await panel_.WaitResultAsync());
        }
    }
}
