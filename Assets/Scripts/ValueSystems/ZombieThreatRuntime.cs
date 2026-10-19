using UnityEngine;
using ZombieCardSurvive.Cards.Data;

namespace ZombieCardSurvive.Systems
{
    public class ZombieThreatRuntime
    {
        public ZombieThreatRuntime(
            int runtimeId,
            ZombieCardData sourceCard,
            int firstConsequenceRoundEnd = 1)
        {
            RuntimeId = runtimeId;
            SourceCard = sourceCard;
            DisplayName = sourceCard != null && !string.IsNullOrWhiteSpace(sourceCard.DisplayName)
                ? sourceCard.DisplayName
                : "Zombie";
            Description = sourceCard != null ? sourceCard.Description : string.Empty;
            MaxHealth = sourceCard != null ? sourceCard.Health : 1;
            CurrentHealth = MaxHealth;
            Attack = sourceCard != null ? sourceCard.Attack : 1;
            PermanentMaximumMoraleDamage = sourceCard != null
                ? sourceCard.PermanentMaximumMoraleDamage
                : 0;
            FirstConsequenceRoundEnd = Mathf.Max(1, firstConsequenceRoundEnd);
            Artwork = sourceCard != null ? sourceCard.Artwork : null;
        }

        public int RuntimeId { get; }
        public ZombieCardData SourceCard { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public int MaxHealth { get; }
        public int CurrentHealth { get; private set; }
        public int Attack { get; }
        public int PermanentMaximumMoraleDamage { get; }
        public int FirstConsequenceRoundEnd { get; }
        public Sprite Artwork { get; }
        public bool IsMarkedForKill { get; private set; }
        public int ReservedCombatValue { get; private set; }

        public string NegativeEffectDescription => $"Round end morale damage: {Attack}";

        public bool CanResolveConsequencesAtRoundEnd(int roundIndex)
        {
            return roundIndex >= FirstConsequenceRoundEnd;
        }

        public void MarkForKill(int reservedCombatValue)
        {
            IsMarkedForKill = true;
            ReservedCombatValue = Mathf.Max(0, reservedCombatValue);
        }

        public void ClearKillMark()
        {
            IsMarkedForKill = false;
            ReservedCombatValue = 0;
        }
    }
}
