namespace ZombieCardSurvive.Run
{
    public class RoundEndReport
    {
        public int RoundIndex { get; set; }
        public int TurnIndex { get; set; }
        public int FoodBefore { get; set; }
        public int FoodAfterConsumption { get; set; }
        public int FoodConsumption { get; set; }
        public int FoodShortage { get; set; }
        public int FoodTrimmedByStorage { get; set; }
        public int StorageCapacity { get; set; }
        public int CombatValueBeforeRoundEnd { get; set; }
        public int CombatValueAfterDecay { get; set; }
        public int MaxCombatValue { get; set; }
        public int CombatOvercapLossPercent { get; set; }
        public int CombatOvercapLoss { get; set; }
        public int ZombiesBeforeRoundEnd { get; set; }
        public int ZombiesKilled { get; set; }
        public int ZombiesSurvived { get; set; }
        public int ZombieMoraleDamage { get; set; }
        public int MoraleBefore { get; set; }
        public int MoraleAfter { get; set; }
        public int ShortageMoraleLoss { get; set; }
        public int FoodPlanMoraleDelta { get; set; }
        public int HighMoraleDrift { get; set; }
        public bool PendingFoodPlanApplied { get; set; }
        public string FoodPlanBefore { get; set; }
        public string FoodPlanAfter { get; set; }

        public string BuildSummary()
        {
            return $"Round {RoundIndex} End\n"
                + $"Combat: {CombatValueBeforeRoundEnd} -> {CombatValueAfterDecay}/{MaxCombatValue}, overcap loss {CombatOvercapLoss} ({CombatOvercapLossPercent}%)\n"
                + $"Zombies: {ZombiesBeforeRoundEnd}, killed {ZombiesKilled}, survived {ZombiesSurvived}, morale damage {ZombieMoraleDamage}\n"
                + $"Food: {FoodBefore} - {FoodConsumption} = {FoodAfterConsumption}, shortage {FoodShortage}, trim {FoodTrimmedByStorage}/{StorageCapacity}\n"
                + $"Morale: {MoraleBefore} -> {MoraleAfter}, shortage loss {ShortageMoraleLoss}, plan {FoodPlanMoraleDelta}, drift {HighMoraleDrift}\n"
                + $"Food Plan: {FoodPlanBefore} -> {FoodPlanAfter}";
        }
    }
}
