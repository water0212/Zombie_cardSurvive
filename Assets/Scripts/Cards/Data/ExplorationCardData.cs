using UnityEngine;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Exploration.Data;
using ZombieCardSurvive.Exploration.Runtime;

namespace ZombieCardSurvive.Cards.Data
{
    [CreateAssetMenu(fileName = "ExplorationCard", menuName = "Zombie Card Survive/Cards/Exploration Card")]
    public class ExplorationCardData : CardBase
    {
        [Header("Exploration")]
        [SerializeField] private ExplorationTier explorationTier = ExplorationTier.Basic;
        [SerializeField] private int explorationMaxUsesPerRun = 1;
        [SerializeField] private int regionOfferCount = 3;
        [SerializeField] private float basicRegionMultiplier = 1f;
        [SerializeField] private float advancedRegionMultiplier = 0.15f;
        [SerializeField] private float highRiskRegionMultiplier;

        public ExplorationTier ExplorationTier => explorationTier;
        public override bool HasLimitedUses => true;
        public override int MaxUsesPerRun => Mathf.Max(1, explorationMaxUsesPerRun);
        public int RegionOfferCount => Mathf.Max(1, regionOfferCount);
        public override bool CreatesExhaustionRefillRequest => true;

        public float GetRegionTierMultiplier(ExplorationTier regionTier)
        {
            switch (regionTier)
            {
                case ExplorationTier.Basic:
                    return Mathf.Max(0f, basicRegionMultiplier);
                case ExplorationTier.Advanced:
                    return Mathf.Max(0f, advancedRegionMultiplier);
                case ExplorationTier.HighRisk:
                    return Mathf.Max(0f, highRiskRegionMultiplier);
                default:
                    return 0f;
            }
        }

        public override void Resolve(CardPlayContext context)
        {
            base.Resolve(context);
            ExplorationSystem.BeginExploration(this, context.Controller);
        }
    }
}
