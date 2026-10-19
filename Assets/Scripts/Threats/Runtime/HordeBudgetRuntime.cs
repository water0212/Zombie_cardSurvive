using System;
using UnityEngine;
using ZombieCardSurvive.Threats.Data;

namespace ZombieCardSurvive.Threats.Runtime
{
    public sealed class HordeBudgetRuntime
    {
        public HordeBudgetSnapshot LastSnapshot { get; private set; }

        public HordeBudgetSnapshot Capture(
            HordeProfileData profile,
            HordeBudgetInputSnapshot input)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            int baseBudget = profile.BaseBudget;
            int worldThreatBudget = Mathf.Max(0, input.WorldThreatTier - 1)
                * profile.WorldThreatBudgetPerTier;
            int baseAttentionBudget = Mathf.FloorToInt(
                input.BaseAttention * profile.BaseAttentionBudgetPerPoint);
            int regionPressureBudget = Mathf.FloorToInt(
                input.RegionPressure * profile.RegionPressureBudgetMultiplier);
            int rawBudget = baseBudget
                + worldThreatBudget
                + baseAttentionBudget
                + regionPressureBudget
                + input.EventBonusBudget;
            int finalBudget = Mathf.Max(profile.MinimumBudget, rawBudget);
            if (profile.MaximumBudget > 0)
            {
                finalBudget = Mathf.Min(finalBudget, profile.MaximumBudget);
            }

            finalBudget = Mathf.Max(0, finalBudget);
            LastSnapshot = new HordeBudgetSnapshot(
                profile,
                input,
                baseBudget,
                worldThreatBudget,
                baseAttentionBudget,
                regionPressureBudget,
                input.EventBonusBudget,
                rawBudget,
                finalBudget);
            return LastSnapshot;
        }
    }
}
