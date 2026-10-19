using ZombieCardSurvive.Threats.Data;

namespace ZombieCardSurvive.Threats.Runtime
{
    public sealed class HordeBudgetSnapshot
    {
        public HordeBudgetSnapshot(
            HordeProfileData profile,
            HordeBudgetInputSnapshot input,
            int baseBudget,
            int worldThreatBudget,
            int baseAttentionBudget,
            int regionPressureBudget,
            int eventBonusBudget,
            int rawBudget,
            int finalBudget)
        {
            Profile = profile;
            Input = input;
            BaseBudget = baseBudget;
            WorldThreatBudget = worldThreatBudget;
            BaseAttentionBudget = baseAttentionBudget;
            RegionPressureBudget = regionPressureBudget;
            EventBonusBudget = eventBonusBudget;
            RawBudget = rawBudget;
            FinalBudget = finalBudget;
        }

        public HordeProfileData Profile { get; }
        public HordeBudgetInputSnapshot Input { get; }
        public int BaseBudget { get; }
        public int WorldThreatBudget { get; }
        public int BaseAttentionBudget { get; }
        public int RegionPressureBudget { get; }
        public int EventBonusBudget { get; }
        public int RawBudget { get; }
        public int FinalBudget { get; }
    }
}
