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
                + $"Food: {FoodBefore} - {FoodConsumption} = {FoodAfterConsumption}, shortage {FoodShortage}, trim {FoodTrimmedByStorage}/{StorageCapacity}\n"
                + $"Morale: {MoraleBefore} -> {MoraleAfter}, shortage loss {ShortageMoraleLoss}, plan {FoodPlanMoraleDelta}, drift {HighMoraleDrift}\n"
                + $"Food Plan: {FoodPlanBefore} -> {FoodPlanAfter}";
        }
    }
}
