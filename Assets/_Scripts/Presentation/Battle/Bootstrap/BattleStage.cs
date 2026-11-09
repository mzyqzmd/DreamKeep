using System.Collections.Generic;
using UnityEngine;

namespace _DreamKeep.Presentation
{
    /// <summary>战斗段表现句柄：常驻层够到当前战斗段表现锚点（战场单位根、目标裁决中间件与战斗交互层）的唯一通道。
    /// 与 BattleSession 成对开关、由段侧装配同一处维护：开段先 stage 后 session（重绑时表现引用已就绪），
    /// 关段先 session 后 stage（药水栏借旧中间件注销瞄准时旧引用仍有效）。段不在场时引用为空，消费方自行判空。</summary>
    public sealed class BattleStage
    {
        public UI_ActorRoot ActorRoot { get; private set; }
        public BattleTargetDirector TargetDirector { get; private set; }

        // 战斗交互层（能量/结束回合/牌堆/手牌/牌堆查看）：段装配期由序列化引用传入，运行时零查找
        private IReadOnlyList<GameObject> _hudLayers;

        #region 开关会话：段装配完成时写入，段收尾时清空

        public void Open(UI_ActorRoot actorRoot_, BattleTargetDirector targetDirector_,
            IReadOnlyList<GameObject> hudLayers_)
        {
            ActorRoot = actorRoot_;
            TargetDirector = targetDirector_;
            _hudLayers = hudLayers_;
        }

        public void Close()
        {
            ActorRoot = null;
            TargetDirector = null;
            _hudLayers = null;
        }

        #endregion

        #region 收起战斗交互层：战斗完成与领奖恢复共用，幂等；引用由段装配期传入，不做名字查找

        public void HideHud()
        {
            if (_hudLayers == null) return;
            for (int i = 0; i < _hudLayers.Count; i++)
                if (_hudLayers[i]) _hudLayers[i].SetActive(false);
        }

        #endregion
    }
}
