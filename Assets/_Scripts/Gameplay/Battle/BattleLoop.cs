using System.Collections.Generic;
using _DreamKeep.RuntimeData;
using _DreamKeep.Utility;

namespace _DreamKeep.Gameplay
{
    /// <summary>战斗循环：以状态机驱动整场战斗的生命周期（战斗开始 → 六回合阶段 → 战斗结束），也是表现层唯一的命令入口。</summary>
    public class BattleLoop
    {
        // 六阶段一圈最多 6 次切换，取两倍作为推进上限，防止某个阶段漏设 IsWaiting 时锁死主线程
        private const int MaxAdvanceSteps = 12;

        private readonly StateMachine<BaseState<BattleLoop>> _fsm = new();
        private readonly BattleState _battleState;
        private readonly DeckService _deckService;
        private readonly ActionSystem _actionSystem;
        private readonly HookRegistry _hooks;
        private readonly BattleEventBuffer _events;
        private readonly AffixToActionBaker _affixFactory;

        public BattleState BattleState => _battleState;
        public ActionSystem ActionSystem => _actionSystem;
        public HookRegistry Hooks => _hooks;
        public BattleEventBuffer Events => _events;
        public AffixToActionBaker AffixFactory => _affixFactory;
        public int DrawPerTurn { get; }
        /// <summary>是否停在等待命令的阶段，表现层据此决定能否发命令。</summary>
        public bool IsWaitingForCommand => _fsm.CurrentState is BP_PlayerTurnActive;

        public BattlePhase PhaseBattleStart { get; }
        public BattlePhase PhasePlayerTurnStart { get; }
        public BattlePhase PhasePlayerTurnActive { get; }
        public BattlePhase PhasePlayerTurnEnd { get; }
        public BattlePhase PhaseEnemyTurnStart { get; }
        public BattlePhase PhaseEnemyTurnActive { get; }
        public BattlePhase PhaseEnemyTurnEnd { get; }
        public BattlePhase PhaseBattleFinish { get; }

        public DeckService DeckService => _deckService;

        public BattleLoop(BattleState battleState_, HookRegistry hooks_, BattleEventBuffer events_, DeckService deckService_, int drawPerTurn_)
        {
            _battleState = battleState_;
            _deckService = deckService_;
            _hooks = hooks_;
            _events = events_;
            DrawPerTurn = drawPerTurn_;

            _actionSystem = new ActionSystem(battleState_, hooks_, events_);
            _affixFactory = new AffixToActionBaker(_actionSystem, new TargetResolver());

            PhaseBattleStart = new BP_BattleStart(_fsm, this);
            PhasePlayerTurnStart = new BP_PlayerTurnStart(_fsm, this);
            PhasePlayerTurnActive = new BP_PlayerTurnActive(_fsm, this);
            PhasePlayerTurnEnd = new BP_PlayerTurnEnd(_fsm, this);
            PhaseEnemyTurnStart = new BP_EnemyTurnStart(_fsm, this);
            PhaseEnemyTurnActive = new BP_EnemyTurnActive(_fsm, this);
            PhaseEnemyTurnEnd = new BP_EnemyTurnEnd(_fsm, this);
            PhaseBattleFinish = new BP_BattleFinish(_fsm, this);
        }

        #region 启动战斗：进入战斗开始阶段，由该阶段触发开始钩子与事件后自动推进
        public void Start()
        {
            _fsm.Initialize(PhaseBattleStart);
            Advance();
        }
        #endregion

        #region 命令入口：表现层唯一的驱动方式，命令合法性由 Domain 校验
        public void ExecuteCommand(InBattleCommand command_)
        {
            if (_battleState.IsBattleOver) return;

            switch (command_)
            {
                case IBC_PlayCard playCard:
                    if (!(_fsm.CurrentState is BP_PlayerTurnActive active)) return;
                    active.PlayCard(playCard.PlayedCard, playCard.AimSlot);
                    Advance();
                    break;
                case IBC_PlayerEndTurn _:
                    if (!(_fsm.CurrentState is BP_PlayerTurnActive)) return;
                    _fsm.ChangeState(PhasePlayerTurnEnd);
                    Advance();
                    break;
            }
        }
        #endregion

        // 外层推进：同步跑完非等待阶段，遇等待阶段或战斗结束停下
        private void Advance()
        {
            for (int step = 0; step < MaxAdvanceSteps; step++)
            {
                if (_battleState.IsBattleOver)
                {
                    _fsm.ChangeState(PhaseBattleFinish);
                    return;
                }
                BattlePhase current = _fsm.CurrentState as BattlePhase;
                if (current == null || current.IsWaiting) return;
                _fsm.ChangeState(current.Next);
            }
        }

        #region 装配一场战斗：卡组原型逐张克隆为可变副本，战斗内改动只落在副本上

        public static BattleState Create(IReadOnlyDictionary<int, CardModel> deck_, Player player_, List<Enemy> enemies_, Rng rng_, int energyPerTurn_)
        {
            List<CardModel> battleDeck = new(deck_.Count);
            Dictionary<CardModel, int> sourceOf = new(deck_.Count);
            // 卡组字典仅用于克隆与建映射、无排列语义，顺序不敏感故可直接枚举
            foreach (KeyValuePair<int, CardModel> pair in deck_)
            {
                CardModel copy = pair.Value.ToMutable();
                battleDeck.Add(copy);
                sourceOf[copy] = pair.Key;
            }
            return new(player_, enemies_, battleDeck, sourceOf, rng_, energyPerTurn_);
        }

        #endregion

        #region 构造一个敌人：当前敌人无行动能力，意图待将来敌人 AI 落地后再接入

        public static Enemy CreateEnemy(int health_, int block_, int slot_) => new(health_, block_, slot_);

        #endregion
    }
}
