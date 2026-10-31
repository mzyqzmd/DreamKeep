using System.Collections.Generic;
using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>配置意图烘焙器：把敌人配置里的意图转成运行时意图，复合意图递归展开，运行时不持有配置引用。</summary>
    public static class IntentBaker
    {
        #region 按配置烘焙一串运行时意图，无法识别的配置直接跳过

        public static List<EnemyIntent> Bake(List<IntentData> configs_)
        {
            List<EnemyIntent> intents = new();
            if (configs_ == null) return intents;

            for (int i = 0; i < configs_.Count; i++)
            {
                EnemyIntent intent = Bake(configs_[i]);
                if (intent != null) intents.Add(intent);
            }

            return intents;
        }
        #endregion

        #region 烘焙单条配置意图：复合意图递归展开为独立运行时实例

        public static EnemyIntent Bake(IntentData data_)
        {
            return data_ switch
            {
                DamageIntentData damage => new EI_Damage(damage.Damage, damage.Times),
                BlockIntentData block => new EI_Block(block.Amount),
                ApplyPerkIntentData applyPerk => ToApplyPerk(applyPerk),
                MixedIntentData mixed => new EI_Mixed(Bake(mixed.Intent1), Bake(mixed.Intent2)),
                MoveIntentData => new EI_Move(),
                StunIntentData => new EI_Stun(),
                HealIntentData heal => new EI_Heal(heal.Amount),
                _ => null,
            };
        }

        #endregion

        /// <summary>状态从配置模板烘出独立运行时实例，再叠加意图给出的层数。</summary>
        private static EnemyIntent ToApplyPerk(ApplyPerkIntentData data_)
        {
            if (data_.Perk == null) return null;
            BattlePerk perk = PerkBaker.Bake(data_.Perk);
            if (perk == null) return null;
            perk.AddLayers(data_.Layers);
            return new EI_ApplyPerk(perk, data_.IsSelf);
        }
    }
}
