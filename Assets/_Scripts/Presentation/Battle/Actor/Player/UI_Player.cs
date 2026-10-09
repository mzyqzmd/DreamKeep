using _DreamKeep.ConfigData;
using _DreamKeep.GameBattle;
using _DreamKeep.RuntimeData;
using Cysharp.Threading.Tasks;

namespace _DreamKeep.Presentation
{
    /// <summary>玩家单位视图：全场唯一，由生成侧在 Instantiate 后装配；没有意图、也不参与阵型移动。
    /// 四条公约动画（idle_loop/attack/hurt/die）走 UI_Actor，本类只声明 ironclad 的专属动画。</summary>
    public sealed class UI_Player : UI_Actor
    {
        // ironclad 专属动画：cast 已接入（出技能/能力牌时播）；两条 relaxed（闲置放松）尚未接入演出
        private const string CastAnim = "cast";

        #region 装配入口：由生成侧在 Instantiate 后装配

        public void Bind(BattleEventDispatcher eventDispatcher_, Player player_)
        {
            base.Bind(eventDispatcher_, player_.Creature);
            _animTool.VerifyAnim(CastAnim);
            eventDispatcher_.Register<BE_CardPlayed>(OnCardPlayedAsync);
        }

        #endregion

        // 出牌手势：只有技能/能力牌会施法（攻击卡静默，挥拳由伤害事件驱动；敌人今日不出牌，故不滤来源）
        private async UniTask OnCardPlayedAsync(BE_CardPlayed played_)
        {
            if (_boundCreature.IsDead) return;
            ECardType type = played_.Card.Type;
            if (type != ECardType.Skill && type != ECardType.Ability) return;
            await _animTool.PlayOnceAsync(CastAnim);
            _animTool.PlayLoop(IdleLoopAnim);
        }
    }
}