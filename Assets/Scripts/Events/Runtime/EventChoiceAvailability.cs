namespace ZombieCardSurvive.Events.Runtime
{
    public enum EventChoiceBlockReason
    {
        None,
        InsufficientPermanentFood,
        InsufficientResource,
        EffectUnavailable,
        InvalidConfiguration
    }

    public sealed class EventChoiceAvailability
    {
        public EventChoiceAvailability(
            bool isAvailable,
            EventChoiceBlockReason blockReason,
            int requiredFood,
            int availableFood,
            int requiredResource,
            int availableResource,
            string detail = "")
        {
            IsAvailable = isAvailable;
            BlockReason = blockReason;
            RequiredFood = requiredFood;
            AvailableFood = availableFood;
            RequiredResource = requiredResource;
            AvailableResource = availableResource;
            Detail = detail ?? string.Empty;
        }

        public bool IsAvailable { get; }
        public EventChoiceBlockReason BlockReason { get; }
        public int RequiredFood { get; }
        public int AvailableFood { get; }
        public int RequiredResource { get; }
        public int AvailableResource { get; }
        public string Detail { get; }
        public bool HasFoodShortage => AvailableFood < RequiredFood;
        public bool HasResourceShortage => AvailableResource < RequiredResource;
    }
}
