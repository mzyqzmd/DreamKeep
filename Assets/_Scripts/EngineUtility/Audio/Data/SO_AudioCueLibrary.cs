using UnityEngine;

namespace _DreamKeep.EngineUtility
{
    /// <summary>音频配置库：一声音一 cue 的具名引用集中在这一份资产里，由 AudioRoot 持并暴露 Cues。
    /// 消费点只注入 AudioRoot、读 Cues 上的具名属性，不再各持音频字段；本类不解释语义、不提供查表。
    /// MerchantThanks 一个 cue 内放 Thanks / Laugh 两条 clip 变体，是全工程唯一多 clip 的 cue。</summary>
    public sealed class SO_AudioCueLibrary : ScriptableObject
    {
        [Header("BGM")] [SerializeField] private SO_AudioCue _bgmMainMenu;
        [SerializeField] private SO_AudioCue _bgmRun;
        [SerializeField] private SO_AudioCue _bgmBattle;
        [SerializeField] private SO_AudioCue _bgmBoss;

        [Header("全局交互")] [SerializeField] private SO_AudioCue _uiClick;
        [SerializeField] private SO_AudioCue _uiReject;

        [Header("战斗")] [SerializeField] private SO_AudioCue _cardDraw;
        [SerializeField] private SO_AudioCue _cardPlay;
        [SerializeField] private SO_AudioCue _hit;
        [SerializeField] private SO_AudioCue _playerHurt;
        [SerializeField] private SO_AudioCue _block;
        [SerializeField] private SO_AudioCue _heal;
        [SerializeField] private SO_AudioCue _perkApply;
        [SerializeField] private SO_AudioCue _actorDeath;
        [SerializeField] private SO_AudioCue _turnStart;
        [SerializeField] private SO_AudioCue _turnEnd;
        [SerializeField] private SO_AudioCue _potion;
        [SerializeField] private SO_AudioCue _removeCard;

        [Header("终局")] [SerializeField] private SO_AudioCue _victory;
        [SerializeField] private SO_AudioCue _defeat;

        [Header("跑图与奖励")] [SerializeField] private SO_AudioCue _cardGain;
        [SerializeField] private SO_AudioCue _chest;
        [SerializeField] private SO_AudioCue _forge;
        [SerializeField] private SO_AudioCue _gold;
        [SerializeField] private SO_AudioCue _itemGain;

        [Header("商人")] [SerializeField] private SO_AudioCue _merchantWelcome;
        [SerializeField] private SO_AudioCue _merchantThanks;
        [SerializeField] private SO_AudioCue _merchantDisappoint;
        [SerializeField] private SO_AudioCue _merchantHum;
        [SerializeField] private SO_AudioCue _merchantPassive;

        /// <summary>主菜单 BGM：启动进主菜单时播、整局结束回到主菜单时恢复。</summary>
        public SO_AudioCue BgmMainMenu => _bgmMainMenu;

        /// <summary>跑图 BGM：进局建起整局容器时起播；非战斗段与回到地图时切回它。</summary>
        public SO_AudioCue BgmRun => _bgmRun;

        /// <summary>战斗 BGM：进入普通战斗段时压过跑图曲。</summary>
        public SO_AudioCue BgmBattle => _bgmBattle;

        /// <summary>Boss 战 BGM：进入 Boss 层战斗段时用，替代普通战斗曲。</summary>
        public SO_AudioCue BgmBoss => _bgmBoss;

        /// <summary>UI 点击：全局点击拦截器统一播（白名单控件），地图上点节点也走它。</summary>
        public SO_AudioCue UiClick => _uiClick;

        /// <summary>操作被拒：出牌被拒绝时播，与点击音区分开。</summary>
        public SO_AudioCue UiReject => _uiReject;

        /// <summary>抽牌：同帧内多张只播一次。</summary>
        public SO_AudioCue CardDraw => _cardDraw;

        /// <summary>出牌：打出一张牌时播。</summary>
        public SO_AudioCue CardPlay => _cardPlay;

        /// <summary>命中：敌人受击且未被格挡完全吸收时播。</summary>
        public SO_AudioCue Hit => _hit;

        /// <summary>玩家受伤：玩家受击且未被格挡完全吸收时播，替代命中音。</summary>
        public SO_AudioCue PlayerHurt => _playerHurt;

        /// <summary>格挡：任一方获得格挡时播。</summary>
        public SO_AudioCue Block => _block;

        /// <summary>回血：任一方受到治疗时播。</summary>
        public SO_AudioCue Heal => _heal;

        /// <summary>施加状态：任一方被施加（或叠加）状态时播。</summary>
        public SO_AudioCue PerkApply => _perkApply;

        /// <summary>单位死亡：任一方死亡时播。</summary>
        public SO_AudioCue ActorDeath => _actorDeath;

        /// <summary>回合开始：仅玩家回合开始时播，敌人回合不播。</summary>
        public SO_AudioCue TurnStart => _turnStart;

        /// <summary>回合结束：玩家结束回合时播。</summary>
        public SO_AudioCue TurnEnd => _turnEnd;

        /// <summary>药水消耗：战斗内喝掉一瓶药水时播。</summary>
        public SO_AudioCue Potion => _potion;

        /// <summary>删牌：删牌面板确认删掉一张卡时播。</summary>
        public SO_AudioCue RemoveCard => _removeCard;

        /// <summary>胜利：结算面板翻出通关结果时播。</summary>
        public SO_AudioCue Victory => _victory;

        /// <summary>战败：结算面板翻出失败结果时播。</summary>
        public SO_AudioCue Defeat => _defeat;

        /// <summary>获得卡：奖励或事件把卡加进牌组时播。</summary>
        public SO_AudioCue CardGain => _cardGain;

        /// <summary>开宝箱：宝箱开启演出起始时播，域要等这段演出放行后才出遗物。</summary>
        public SO_AudioCue Chest => _chest;

        /// <summary>锻造：升级面板确认升级一张卡时播。</summary>
        public SO_AudioCue Forge => _forge;

        /// <summary>金币：整局金币数值变化时播（进账与花费都走它）。</summary>
        public SO_AudioCue Gold => _gold;

        /// <summary>获得遗物：领到一件遗物时播，遗物栏当场补一格。</summary>
        public SO_AudioCue ItemGain => _itemGain;

        /// <summary>商人招呼：商店段开启（SE_ShopOpened）时播一次。</summary>
        public SO_AudioCue MerchantWelcome => _merchantWelcome;

        /// <summary>商人道谢：购买成功时随台词播；一个 cue 内含 Thanks / Laugh 两条变体。</summary>
        public SO_AudioCue MerchantThanks => _merchantThanks;

        /// <summary>商人失望：购买被拒或删牌被拒时随台词播。</summary>
        public SO_AudioCue MerchantDisappoint => _merchantDisappoint;

        /// <summary>商人哼声：请求删牌面板打开时播，作为商人开口的音效。</summary>
        public SO_AudioCue MerchantHum => _merchantHum;

        /// <summary>商人待机：进店收摊后播一次，作为商店营业的背景人声。</summary>
        public SO_AudioCue MerchantPassive => _merchantPassive;
    }
}
