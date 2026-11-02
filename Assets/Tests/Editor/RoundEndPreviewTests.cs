using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ZombieCardSurvive.Buildings.Data;
using ZombieCardSurvive.Buildings.Runtime;
using ZombieCardSurvive.Cards.Data;
using ZombieCardSurvive.Run;
using ZombieCardSurvive.Systems;
using ZombieCardSurvive.UI.Morale;

namespace ZombieCardSurvive.Tests.Editor
{
    public sealed class RoundEndPreviewTests
    {
        [SetUp]
        public void SetUp()
        {
            ResourceReservationService.Reset();
            FoodSystem.Reset(0);
            FoodSystem.SetStorageCapacity(50);
            ResourceSystem.Reset(0);
            EnergySystem.Reset(3);
            CombatSystem.Reset();
            MoraleSystem.Reset();
            MoralePyramidSystem.Reset();
            FoodPlanSystem.Reset();
            NextRoundEffectSystem.Reset();
            GameplayInteractionLock.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            ResourceReservationService.Reset();
            CombatSystem.Reset();
            MoralePyramidSystem.Reset();
            NextRoundEffectSystem.Reset();
            GameplayInteractionLock.Reset();
        }

        [Test]
        public void Capture_RepeatedCalls_DoNotMutateRuntimeState()
        {
            FoodSystem.Reset(3);
            FoodSystem.AddTemporaryFood(1);
            ResourceSystem.Reset(4);
            MoraleSystem.Reset(startingMorale: 76);
            int foodBefore = FoodSystem.Food;
            int temporaryBefore = FoodSystem.TemporaryFood;
            int moraleBefore = MoraleSystem.CurrentMorale;

            RoundEndPreview first = RoundEndPreviewService.Capture(1);
            RoundEndPreview second = RoundEndPreviewService.Capture(1);

            Assert.That(first.IsEquivalentTo(second), Is.True);
            Assert.That(FoodSystem.Food, Is.EqualTo(foodBefore));
            Assert.That(FoodSystem.TemporaryFood, Is.EqualTo(temporaryBefore));
            Assert.That(MoraleSystem.CurrentMorale, Is.EqualTo(moraleBefore));
            Assert.That(MoraleSystem.CollapseStage, Is.EqualTo(MoraleCollapseStage.Stable));
        }

        [Test]
        public void Capture_DoesNotNotifyOrChangeReservationsAndSchedules()
        {
            FoodSystem.Reset(10);
            ResourceSystem.Reset(10);
            Assert.That(ResourceReservationService.TryReserve(new ResourceCost(4, 3), out _), Is.True);
            int notifications = 0;
            System.Action onStateChanged = () => notifications++;
            System.Action<MoraleChangeResult> onMoraleChanged = _ => notifications++;
            FoodSystem.StateChanged += onStateChanged;
            ResourceSystem.StateChanged += onStateChanged;
            ResourceReservationService.StateChanged += onStateChanged;
            CombatSystem.StateChanged += onStateChanged;
            MoraleSystem.StateChanged += onMoraleChanged;
            try
            {
                int pendingEffects = NextRoundEffectSystem.Pending.Count;
                RoundEndPreviewService.Capture(1);

                Assert.That(notifications, Is.Zero);
                Assert.That(ResourceReservationService.ReservedFood, Is.EqualTo(4));
                Assert.That(ResourceReservationService.ReservedResource, Is.EqualTo(3));
                Assert.That(NextRoundEffectSystem.Pending.Count, Is.EqualTo(pendingEffects));
            }
            finally
            {
                FoodSystem.StateChanged -= onStateChanged;
                ResourceSystem.StateChanged -= onStateChanged;
                ResourceReservationService.StateChanged -= onStateChanged;
                CombatSystem.StateChanged -= onStateChanged;
                MoraleSystem.StateChanged -= onMoraleChanged;
            }
        }

        [Test]
        public void Capture_FoodShortage_MatchesActualRoundEnd()
        {
            FoodSystem.Reset(2);
            MoraleSystem.Reset(startingMorale: 75);
            RoundEndPreview preview = RoundEndPreviewService.Capture(1);
            GameObject owner = new GameObject("RoundEndPreviewParity");
            RoundEndResolver resolver = owner.AddComponent<RoundEndResolver>();
            try
            {
                RoundEndReport report = resolver.ResolveRoundEnd(null, 1, 3);
                Assert.That(preview.FoodShortage, Is.EqualTo(report.FoodShortage));
                Assert.That(preview.ShortageMoraleLoss, Is.EqualTo(report.ShortageMoraleLoss));
                Assert.That(preview.MoraleAfter, Is.EqualTo(report.MoraleAfter));
                Assert.That(preview.StageAfter, Is.EqualTo(report.MoraleStageAfter));
                Assert.That(preview.FoodAfter, Is.EqualTo(report.FoodAfterConsumption));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void Capture_TemporaryAndReservedFood_MatchesActualRoundEnd()
        {
            FoodSystem.Reset(10);
            FoodSystem.AddTemporaryFood(2);
            Assert.That(ResourceReservationService.TryReserve(new ResourceCost(8, 0), out _), Is.True);
            FoodPlanSystem.Reset(baseConsumption: 6);
            MoraleSystem.Reset(startingMorale: 75);
            RoundEndPreview preview = RoundEndPreviewService.Capture(1);
            GameObject owner = new GameObject("RoundEndReservedFoodParity");
            RoundEndResolver resolver = owner.AddComponent<RoundEndResolver>();
            try
            {
                RoundEndReport report = resolver.ResolveRoundEnd(null, 1, 3);
                Assert.That(preview.TemporaryFoodConsumed, Is.EqualTo(report.TemporaryFoodConsumed));
                Assert.That(preview.PermanentFoodConsumed, Is.EqualTo(report.PermanentFoodConsumed));
                Assert.That(preview.FoodShortage, Is.EqualTo(report.FoodShortage));
                Assert.That(preview.MoraleAfter, Is.EqualTo(report.MoraleAfter));
                Assert.That(ResourceReservationService.ReservedFood, Is.EqualTo(8));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void Capture_EncouragingMealSuccessAndCombatDecay_MatchActualRoundEnd()
        {
            FoodPlanSystem.Reset(startingPlan: FoodPlanLevel.Generous, baseConsumption: 5);
            FoodSystem.Reset(20);
            MoraleSystem.Reset(startingMorale: 60);
            CombatSystem.Configure(maxValue: 9, lossPercent: 50);
            CombatSystem.AddPendingDamage(15);
            RoundEndPreview preview = RoundEndPreviewService.Capture(1);
            GameObject owner = new GameObject("RoundEndMealCombatParity");
            RoundEndResolver resolver = owner.AddComponent<RoundEndResolver>();
            try
            {
                RoundEndReport report = resolver.ResolveRoundEnd(null, 1, 3);
                Assert.That(preview.EncouragingMealSucceeded, Is.True);
                Assert.That(preview.EncouragingMealExtraFoodCost, Is.EqualTo(report.EncouragingMealExtraFoodPaid));
                Assert.That(preview.FoodAfter, Is.EqualTo(report.FoodAfterConsumption));
                Assert.That(preview.MoraleAfter, Is.EqualTo(report.MoraleAfter));
                Assert.That(preview.CombatOvercapLoss, Is.EqualTo(report.CombatOvercapLoss));
                Assert.That(preview.CombatAfter, Is.EqualTo(report.CombatValueAfterDecay));
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void Capture_PermanentDamageAndDeferredZombie_MatchActualRoundEnd()
        {
            FoodPlanSystem.Reset(baseConsumption: 0);
            MoraleSystem.Reset(startingMorale: 80);
            CombatSystem.BeginRound(1);
            ZombieCardData eligible = CreateZombie(attack: 7, permanentDamage: 12);
            ZombieCardData deferred = CreateZombie(attack: 40, permanentDamage: 30);
            try
            {
                CombatSystem.SpawnZombieThreat(eligible);
                CombatSystem.BeginRoundEnd(1);
                CombatSystem.SpawnZombieThreat(deferred);
                RoundEndPreview preview = RoundEndPreviewService.Capture(1);
                GameObject owner = new GameObject("RoundEndZombieParity");
                RoundEndResolver resolver = owner.AddComponent<RoundEndResolver>();
                try
                {
                    RoundEndReport report = resolver.ResolveRoundEnd(null, 1, 3);
                    Assert.That(preview.EligibleZombieCount, Is.EqualTo(report.EligibleZombieEffects));
                    Assert.That(preview.DeferredZombieCount, Is.EqualTo(report.DeferredZombieEffects));
                    Assert.That(preview.ZombieMoraleDamage, Is.EqualTo(report.ZombieMoraleDamage));
                    Assert.That(preview.PermanentMaximumMoraleDamage, Is.EqualTo(report.ZombiePermanentMaximumMoraleDamage));
                    Assert.That(preview.MaximumMoraleAfter, Is.EqualTo(MoraleSystem.MaximumMorale));
                    Assert.That(preview.MoraleAfter, Is.EqualTo(report.MoraleAfter));
                }
                finally
                {
                    Object.DestroyImmediate(owner);
                }
            }
            finally
            {
                Object.DestroyImmediate(eligible);
                Object.DestroyImmediate(deferred);
            }
        }

        [Test]
        public void Capture_MarkedZombie_IsExcludedFromKnownConsequences()
        {
            CombatSystem.BeginRound(1);
            ZombieCardData zombie = CreateZombie(attack: 20, permanentDamage: 15);
            try
            {
                ZombieThreatRuntime threat = CombatSystem.SpawnZombieThreat(zombie);
                CombatSystem.AddPendingDamage(1);
                Assert.That(CombatSystem.TryToggleZombieKillMark(threat), Is.True);

                RoundEndPreview preview = RoundEndPreviewService.Capture(1);

                Assert.That(preview.MarkedKillCount, Is.EqualTo(1));
                Assert.That(preview.ZombieMoraleDamage, Is.Zero);
                Assert.That(preview.PermanentMaximumMoraleDamage, Is.Zero);
                Assert.That(preview.CombatBefore, Is.Zero, "標記擊殺時已保留戰鬥值，預測不可再扣一次");
            }
            finally
            {
                Object.DestroyImmediate(zombie);
            }
        }

        [Test]
        public void PermanentDamage_WarningHidesExactAggregateEndpoint()
        {
            FoodPlanSystem.Reset(baseConsumption: 0);
            MoraleSystem.Reset(startingMorale: 72);
            CombatSystem.BeginRound(1);
            ZombieCardData zombie = CreateZombie(attack: 4, permanentDamage: 10);
            try
            {
                CombatSystem.SpawnZombieThreat(zombie);
                RoundEndPreview preview = RoundEndPreviewService.Capture(1);
                string warning = MoraleWarningFormatter.BuildMoraleWarning(preview);

                Assert.That(preview.HasPermanentDamage, Is.True);
                Assert.That(preview.WillCrossStage, Is.True);
                Assert.That(warning, Does.Contain("動搖"));
                Assert.That(warning, Does.Not.Contain(preview.MoraleAfter.ToString()));
                Assert.That(warning, Does.Not.Contain(preview.PermanentMaximumMoraleDamage.ToString()));
            }
            finally
            {
                Object.DestroyImmediate(zombie);
            }
        }

        [Test]
        public void EncouragingMeal_ReportsActualFailureReason()
        {
            FoodPlanSystem.Reset(startingPlan: FoodPlanLevel.Generous, baseConsumption: 5);
            FoodSystem.Reset(5);
            MoraleSystem.Reset(startingMorale: 60);

            RoundEndPreview preview = RoundEndPreviewService.Capture(1);

            Assert.That(preview.EncouragingMealAttempted, Is.True);
            Assert.That(preview.EncouragingMealSucceeded, Is.False);
            Assert.That(preview.EncouragingMealFailureReason,
                Is.EqualTo(EncouragingMealFailureReason.InsufficientPermanentFood));
            Assert.That(MoraleWarningFormatter.BuildFoodWarning(preview), Does.Contain("可用永久食物不足"));
        }

        private static ZombieCardData CreateZombie(int attack, int permanentDamage)
        {
            ZombieCardData zombie = ScriptableObject.CreateInstance<ZombieCardData>();
            SerializedObject serialized = new SerializedObject(zombie);
            serialized.FindProperty("health").intValue = 1;
            serialized.FindProperty("attack").intValue = attack;
            serialized.FindProperty("permanentMaximumMoraleDamage").intValue = permanentDamage;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return zombie;
        }
    }
}
