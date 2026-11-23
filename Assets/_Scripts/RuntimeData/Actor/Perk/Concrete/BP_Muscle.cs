using System.Collections.Generic;
using _DreamKeep.ConfigData;

namespace _DreamKeep.RuntimeData
{
    /// <summary>状态「肌肉」运行时实例：层数是同步给宿主的真实力量层；本回合结束整条移除并扣回。</summary>
    public sealed class BP_Muscle : BattlePerk
    {
        private int _syncedLayers;

        private SO_Muscle Config => (SO_Muscle)_data;

        public BP_Muscle(SO_Muscle data_) : base(data_) { }

        /// <summary>按层数差增量同步真实力量，避免累加时重复挂载。</summary>
        public override void OnMounted(Creature host_)
        {
            int delta = Layers - _syncedLayers;
            if (delta == 0) return;

            BP_Strength real = new(Config.Strength);
            real.AddLayers(delta);
            host_.ApplyPerk(real);
            _syncedLayers = Layers;
        }

        /// <summary>卸载时按已同步层数扣回真实力量，归零则移除该状态实例。</summary>
        public override void OnUnmounted(Creature host_)
        {
            if (_syncedLayers == 0) return;

            IReadOnlyList<BattlePerk> perks = host_.Perks;
            for (int i = 0; i < perks.Count; i++)
            {
                if (perks[i] is not BP_Strength real) continue;
                real.AddLayers(-_syncedLayers);
                if (real.Layers <= 0) host_.RemovePerk(real);
                break;
            }
            _syncedLayers = 0;
        }

        public override bool Settle() => true;

        public override string GetDescription() => $"本回合临时获得 {Layers} 点力量，\n回合结束时失去";

        public override BattlePerk Clone() => WithLayers(new BP_Muscle(Config));
    }
}
