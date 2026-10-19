using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZombieCardSurvive.Systems
{
    public enum MoraleStageEffectType
    {
        LoseResourceEachRound,
        ModifyMaximumEnergy
    }

    [Serializable]
    public sealed class MoraleStageEffectEntry
    {
        [SerializeField] private MoraleStageEffectType effectType;
        [SerializeField] private int amount;

        public MoraleStageEffectType EffectType => effectType;
        public int Amount => amount;
    }

    [Serializable]
    public sealed class MoraleStageDefinition
    {
        [SerializeField] private string displayName;
        [TextArea]
        [SerializeField] private string description;
        [SerializeField] private List<MoraleStageEffectEntry> effects =
            new List<MoraleStageEffectEntry>();

        public string DisplayName => displayName ?? string.Empty;
        public string Description => description ?? string.Empty;
        public IReadOnlyList<MoraleStageEffectEntry> Effects => effects;

        internal bool TryCreateSnapshot(
            MoraleCollapseStage stage,
            out MoraleStageSnapshot snapshot,
            out string error)
        {
            long resourceLossPerRound = 0;
            long maximumEnergyModifier = 0;

            if (effects != null)
            {
                for (int index = 0; index < effects.Count; index++)
                {
                    MoraleStageEffectEntry effect = effects[index];
                    if (effect == null)
                    {
                        snapshot = null;
                        error = $"{stage} contains a null effect entry at index {index}.";
                        return false;
                    }

                    switch (effect.EffectType)
                    {
                        case MoraleStageEffectType.LoseResourceEachRound:
                            if (effect.Amount < 0)
                            {
                                snapshot = null;
                                error = $"{stage} resource loss cannot be negative.";
                                return false;
                            }

                            resourceLossPerRound += effect.Amount;
                            break;
                        case MoraleStageEffectType.ModifyMaximumEnergy:
                            maximumEnergyModifier += effect.Amount;
                            break;
                        default:
                            snapshot = null;
                            error = $"{stage} contains unsupported effect type '{effect.EffectType}'.";
                            return false;
                    }
                }
            }

            snapshot = new MoraleStageSnapshot(
                stage,
                string.IsNullOrWhiteSpace(displayName) ? stage.ToString() : displayName,
                description,
                ClampToInt(resourceLossPerRound, 0, int.MaxValue),
                ClampToInt(maximumEnergyModifier, int.MinValue, int.MaxValue));
            error = null;
            return true;
        }

        private static int ClampToInt(long value, int minimum, int maximum)
        {
            return (int)Math.Max(minimum, Math.Min(maximum, value));
        }
    }

    public sealed class MoraleStageSnapshot
    {
        public MoraleStageSnapshot(
            MoraleCollapseStage stage,
            string displayName,
            string description,
            int resourceLossPerRound,
            int maximumEnergyModifier)
        {
            Stage = stage;
            DisplayName = displayName ?? stage.ToString();
            Description = description ?? string.Empty;
            ResourceLossPerRound = Math.Max(0, resourceLossPerRound);
            MaximumEnergyModifier = maximumEnergyModifier;
        }

        public MoraleCollapseStage Stage { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public int ResourceLossPerRound { get; }
        public int MaximumEnergyModifier { get; }
    }

    public sealed class MoralePyramidProfileSnapshot
    {
        private readonly MoraleStageSnapshot[] stages;

        public MoralePyramidProfileSnapshot(
            string profileId,
            string displayName,
            string description,
            MoraleStageSnapshot stable,
            MoraleStageSnapshot shaken,
            MoraleStageSnapshot brink)
        {
            ProfileId = profileId;
            DisplayName = displayName;
            Description = description;
            stages = new[] { stable, shaken, brink };
        }

        public string ProfileId { get; }
        public string DisplayName { get; }
        public string Description { get; }

        public MoraleStageSnapshot GetStage(MoraleCollapseStage stage)
        {
            int index = Mathf.Clamp((int)stage, 0, stages.Length - 1);
            return stages[index];
        }
    }

    [CreateAssetMenu(
        fileName = "MoralePyramidProfile",
        menuName = "Zombie Card Survive/Morale/Pyramid Profile")]
    public sealed class MoralePyramidProfileData : ScriptableObject
    {
        [SerializeField] private string profileId;
        [SerializeField] private string displayName;
        [TextArea]
        [SerializeField] private string description;
        [SerializeField] private MoraleStageDefinition stage0 = new MoraleStageDefinition();
        [SerializeField] private MoraleStageDefinition stage1 = new MoraleStageDefinition();
        [SerializeField] private MoraleStageDefinition stage2 = new MoraleStageDefinition();

        public string ProfileId => profileId ?? string.Empty;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? ProfileId : displayName;
        public string Description => description ?? string.Empty;
        public MoraleStageDefinition Stage0 => stage0;
        public MoraleStageDefinition Stage1 => stage1;
        public MoraleStageDefinition Stage2 => stage2;

        public bool TryCreateSnapshot(
            out MoralePyramidProfileSnapshot snapshot,
            out string error)
        {
            snapshot = null;
            if (string.IsNullOrWhiteSpace(profileId))
            {
                error = "Profile ID is required.";
                return false;
            }

            if (stage0 == null || stage1 == null || stage2 == null)
            {
                error = $"Profile '{profileId}' must define Stage 0, Stage 1, and Stage 2.";
                return false;
            }

            if (!stage0.TryCreateSnapshot(MoraleCollapseStage.Stable, out MoraleStageSnapshot stable, out error)
                || !stage1.TryCreateSnapshot(MoraleCollapseStage.Shaken, out MoraleStageSnapshot shaken, out error)
                || !stage2.TryCreateSnapshot(MoraleCollapseStage.Brink, out MoraleStageSnapshot brink, out error))
            {
                return false;
            }

            snapshot = new MoralePyramidProfileSnapshot(
                profileId,
                DisplayName,
                description,
                stable,
                shaken,
                brink);
            error = null;
            return true;
        }
    }
}
