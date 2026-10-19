using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Shared.AutoId;

namespace ZombieCardSurvive.Threats.Data
{
    [CreateAssetMenu(fileName = "HordeProfile", menuName = "Zombie Card Survive/Threats/Horde Profile")]
    public sealed class HordeProfileData : ScriptableObject, IAutoIdAsset
    {
        [SerializeField] private string profileId;
        [SerializeField] private string displayName;
        [TextArea]
        [SerializeField] private string description;
        [SerializeField] private HordeCompositionStyle compositionStyle = HordeCompositionStyle.Mixed;

        [Header("Budget")]
        [Min(0)]
        [SerializeField] private int baseBudget = 2;
        [Min(0)]
        [SerializeField] private int worldThreatBudgetPerTier = 1;
        [Min(0f)]
        [SerializeField] private float baseAttentionBudgetPerPoint = 1f;
        [Min(0f)]
        [SerializeField] private float regionPressureBudgetMultiplier = 4f;
        [Min(0)]
        [SerializeField] private int minimumBudget;
        [Min(0)]
        [Tooltip("0 means no maximum budget.")]
        [SerializeField] private int maximumBudget;

        [Header("Composition")]
        [SerializeField] private List<ZombieArchetypeEntry> zombiePool = new List<ZombieArchetypeEntry>();

        public string ProfileId => profileId;
        public string DisplayName => displayName;
        public string Description => description;
        public HordeCompositionStyle CompositionStyle => compositionStyle;
        public int BaseBudget => Mathf.Max(0, baseBudget);
        public int WorldThreatBudgetPerTier => Mathf.Max(0, worldThreatBudgetPerTier);
        public float BaseAttentionBudgetPerPoint => Mathf.Max(0f, baseAttentionBudgetPerPoint);
        public float RegionPressureBudgetMultiplier => Mathf.Max(0f, regionPressureBudgetMultiplier);
        public int MinimumBudget => Mathf.Max(0, minimumBudget);
        public int MaximumBudget => Mathf.Max(0, maximumBudget);
        public IReadOnlyList<ZombieArchetypeEntry> ZombiePool => zombiePool;
        public string Id => profileId;

        private void OnValidate()
        {
            EnsureId();
        }

        public void EnsureId()
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                profileId = AutoIdUtility.CreateId("horde");
            }
        }

        public void RegenerateId()
        {
            profileId = AutoIdUtility.CreateId("horde");
        }
    }
}
