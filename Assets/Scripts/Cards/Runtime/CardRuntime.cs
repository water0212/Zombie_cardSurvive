using System;
using ZombieCardSurvive.Cards.Data;

namespace ZombieCardSurvive.Cards.Runtime
{
    [Serializable]
    public class CardRuntime
    {
        public CardRuntime(CardBase data)
            : this(data, GetDefaultRemainingUses(data), null)
        {
        }

        public CardRuntime(CardBase data, int remainingUses, string runtimeId = null)
        {
            RuntimeId = !string.IsNullOrWhiteSpace(runtimeId) ? runtimeId : Guid.NewGuid().ToString("N");
            Data = data;
            RemainingUses = NormalizeRemainingUses(data, remainingUses);
            Zone = DeckZone.Removed;
            AssignedSlotType = DeckSlotType.Unrestricted;
            IsAdditiveInstance = data != null && data.EffectiveIsAdditiveCard;
        }

        public string RuntimeId { get; }
        public CardBase Data { get; }
        public int RemainingUses { get; private set; }
        public DeckZone Zone { get; private set; }
        public DeckSlotType AssignedSlotType { get; private set; }
        public bool IsAdditiveInstance { get; private set; }
        public bool HasRemainingUses => Data == null || !Data.HasLimitedUses || RemainingUses > 0;
        public bool IsExhausted => Zone == DeckZone.Exhausted;

        public void SetZone(DeckZone zone)
        {
            Zone = zone;
        }

        public void SetAssignedSlotType(DeckSlotType slotType)
        {
            AssignedSlotType = slotType;
        }

        public void SetAdditiveInstance(bool isAdditive)
        {
            IsAdditiveInstance = isAdditive;
        }

        public bool ConsumeUse()
        {
            if (Data == null || !Data.HasLimitedUses)
            {
                return false;
            }

            RemainingUses = Math.Max(0, RemainingUses - 1);
            return RemainingUses == 0;
        }

        public void MarkExhausted()
        {
            Zone = DeckZone.Exhausted;
        }

        private static int GetDefaultRemainingUses(CardBase data)
        {
            return data != null && data.HasLimitedUses ? data.MaxUsesPerRun : -1;
        }

        private static int NormalizeRemainingUses(CardBase data, int remainingUses)
        {
            if (data == null || !data.HasLimitedUses)
            {
                return -1;
            }

            if (remainingUses < 0)
            {
                return data.MaxUsesPerRun;
            }

            return Math.Min(data.MaxUsesPerRun, Math.Max(0, remainingUses));
        }
    }
}
