using System;
using System.Collections.Generic;
using _DreamKeep.ConfigData;
using _DreamKeep.RuntimeData;
using UnityEngine;

namespace _DreamKeep.CommonPresentation
{
    /// <summary>一张卡的四张内嵌牌面（攻击 / 技能 / 能力 / 诅咒）：按卡类型切到唯一活跃的那张，同类型重绑零 SetActive 抖动。
    /// 只回答"当前是哪张脸"——牌面内容由使用方各自写入（战斗卡要重置预览台账、牌堆卡要关涟漪），它不介入。</summary>
    public sealed class UI_CardFaceSelector : MonoBehaviour
    {
        [Tooltip("攻击牌面：绑定的卡为攻击类型时切到它")] [SerializeField]
        private UI_CardFace _attackFace;

        [Tooltip("技能牌面：绑定的卡为技能类型时切到它")] [SerializeField]
        private UI_CardFace _skillFace;

        [Tooltip("能力牌面：绑定的卡为能力类型时切到它")] [SerializeField]
        private UI_CardFace _abilityFace;

        [Tooltip("诅咒牌面：绑定的卡为诅咒类型时切到它")] [SerializeField]
        private UI_CardFace _curseFace;

        /// <summary>当前激活的牌面：首次 Select 之前为 null。</summary>
        public UI_CardFace Current { get; private set; }

        #region 按卡类型切换牌面

        /// <summary>切到该卡类型对应的牌面并返回它；该类型漏接引用时抛错，不静默留半成品。</summary>
        public UI_CardFace Select(CardModel card_)
        {
            UI_CardFace face = card_.Type switch
            {
                ECardType.Attack => _attackFace,
                ECardType.Skill => _skillFace,
                ECardType.Ability => _abilityFace,
                ECardType.Curse => _curseFace,
                _ => null
            };

            if (!face)
                throw new Exception(
                    $"[{nameof(UI_CardFaceSelector)}] 卡「{card_.Name}」的类型 {card_.Type} 没有接上对应牌面，牌面未切换");
            if (Current == face) return Current;

            if (Current) Current.gameObject.SetActive(false);
            face.gameObject.SetActive(true);
            Current = face;
            return Current;
        }

        #endregion

        #region 陈列绑定：切脸→刷面→关涟漪→收集词条，一条龙供无预览对账的陈列卡复用

        /// <summary>静态陈列绑定：词条写进 tips_ 后由调用方推给气泡；返回当前牌面。
        /// 战斗卡带预览台账与涟漪，不收敛进这里。</summary>
        public UI_CardFace BindStatic(CardModel card_, List<(string Name, string Description)> tips_)
        {
            UI_CardFace face = Select(card_);
            face.Bind(card_);
            HideRipple(); // 陈列态四张脸一律不播 card_ripple：只关当前脸时，之后切过来的那张会带着动效复活
            face.CollectTipLines(tips_);
            return face;
        }

        // 四张脸全关：未激活的脸也一起处理，切换牌面后不会再把涟漪带回来
        private void HideRipple()
        {
            if (_attackFace) _attackFace.SetRippleVisible(false);
            if (_skillFace) _skillFace.SetRippleVisible(false);
            if (_abilityFace) _abilityFace.SetRippleVisible(false);
            if (_curseFace) _curseFace.SetRippleVisible(false);
        }

        #endregion
    }
}
