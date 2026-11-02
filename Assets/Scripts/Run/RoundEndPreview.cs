using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Run
{
    /// <summary>目前已知狀態的唯讀輪末預測；不包含未抽卡牌或未知事件。</summary>
    public sealed class RoundEndPreview
    {
        public int RoundIndex { get; internal set; }
        public int MoraleBefore { get; internal set; }
        public int MoraleAfter { get; internal set; }
        public int MaximumMoraleBefore { get; internal set; }
        public int MaximumMoraleAfter { get; internal set; }
        public MoraleCollapseStage StageBefore { get; internal set; }
        public MoraleCollapseStage StageAfter { get; internal set; }
        public int FoodBefore { get; internal set; }
        public int FoodAfter { get; internal set; }
        public int FoodDemand { get; internal set; }
        public int FoodShortage { get; internal set; }
        public int TemporaryFoodConsumed { get; internal set; }
        public int PermanentFoodConsumed { get; internal set; }
        public int ShortageMoraleLoss { get; internal set; }
        public bool EncouragingMealAttempted { get; internal set; }
        public bool EncouragingMealSucceeded { get; internal set; }
        public int EncouragingMealExtraFoodCost { get; internal set; }
        public int EncouragingMealMoraleGain { get; internal set; }
        public ZombieCardSurvive.Systems.EncouragingMealFailureReason EncouragingMealFailureReason { get; internal set; }
        public int HighMoraleDrift { get; internal set; }
        public int ResourceBefore { get; internal set; }
        public int ResourceAfter { get; internal set; }
        public int ResourceLoss { get; internal set; }
        public int CombatBefore { get; internal set; }
        public int CombatAfter { get; internal set; }
        public int CombatOvercapLoss { get; internal set; }
        public int ZombieMoraleDamage { get; internal set; }
        public int PermanentMaximumMoraleDamage { get; internal set; }
        public int EligibleZombieCount { get; internal set; }
        public int DeferredZombieCount { get; internal set; }
        public int MarkedKillCount { get; internal set; }

        public bool HasPermanentDamage => PermanentMaximumMoraleDamage > 0;
        public bool WillCrossStage => StageAfter > StageBefore;
        public bool WillCollapse => MoraleAfter <= 0;
        public bool RequiresConfirmation => WillCollapse || WillCrossStage || HasPermanentDamage;

        public bool IsEquivalentTo(RoundEndPreview other)
        {
            if (other == null) return false;
            return RoundIndex == other.RoundIndex
                && MoraleBefore == other.MoraleBefore && MoraleAfter == other.MoraleAfter
                && MaximumMoraleBefore == other.MaximumMoraleBefore && MaximumMoraleAfter == other.MaximumMoraleAfter
                && StageBefore == other.StageBefore && StageAfter == other.StageAfter
                && FoodBefore == other.FoodBefore && FoodAfter == other.FoodAfter
                && FoodDemand == other.FoodDemand && FoodShortage == other.FoodShortage
                && TemporaryFoodConsumed == other.TemporaryFoodConsumed
                && PermanentFoodConsumed == other.PermanentFoodConsumed
                && ShortageMoraleLoss == other.ShortageMoraleLoss
                && ResourceBefore == other.ResourceBefore && ResourceAfter == other.ResourceAfter
                && ResourceLoss == other.ResourceLoss
                && CombatBefore == other.CombatBefore && CombatAfter == other.CombatAfter
                && CombatOvercapLoss == other.CombatOvercapLoss
                && ZombieMoraleDamage == other.ZombieMoraleDamage
                && PermanentMaximumMoraleDamage == other.PermanentMaximumMoraleDamage
                && EligibleZombieCount == other.EligibleZombieCount
                && DeferredZombieCount == other.DeferredZombieCount
                && MarkedKillCount == other.MarkedKillCount
                && EncouragingMealAttempted == other.EncouragingMealAttempted
                && EncouragingMealSucceeded == other.EncouragingMealSucceeded
                && EncouragingMealExtraFoodCost == other.EncouragingMealExtraFoodCost
                && EncouragingMealMoraleGain == other.EncouragingMealMoraleGain
                && HighMoraleDrift == other.HighMoraleDrift
                && EncouragingMealFailureReason == other.EncouragingMealFailureReason;
        }
    }
}
