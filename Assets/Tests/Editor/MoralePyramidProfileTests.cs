using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using ZombieCardSurvive.Buildings.Runtime;
using ZombieCardSurvive.Run;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Tests.Editor
{
    public sealed class MoralePyramidProfileTests
    {
        [SetUp]
        public void SetUp()
        {
            MoralePyramidSystem.Reset();
            MoraleSystem.Reset();
            ResourceReservationService.Reset();
            ResourceSystem.Reset(0);
            FoodSystem.Reset(0);
            EnergySystem.Reset(3);
        }

        [TearDown]
        public void TearDown()
        {
            MoralePyramidSystem.Reset();
            MoraleSystem.Reset();
            ResourceReservationService.Reset();
            ResourceSystem.Reset(0);
            FoodSystem.Reset(0);
            EnergySystem.Reset(0);
        }

        [Test]
        public void PrepareRun_CopiesProfileIntoDetachedSnapshot()
        {
            MoralePyramidProfileData profile = MoralePyramidTestFactory.CreateDemoProfile();
            try
            {
                Assert.That(MoralePyramidSystem.TryPrepareRun(profile, out string error), Is.True, error);

                SerializedObject serialized = new SerializedObject(profile);
                SerializedProperty effects = serialized.FindProperty("stage1")
                    .FindPropertyRelative("effects");
                effects.GetArrayElementAtIndex(0)
                    .FindPropertyRelative("amount").intValue = 9;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                ResourceSystem.Reset(5);
                MoraleSystem.CommitChange("EnterShaken", -31);
                ValueConsumptionResult result =
                    MoralePyramidSystem.ResolveRoundEndResourceLoss(1);

                Assert.That(MoralePyramidSystem.ActiveProfileId, Is.EqualTo("MORALE-PYRAMID-TEST"));
                Assert.That(result.Requested, Is.EqualTo(1));
                Assert.That(ResourceSystem.Resource, Is.EqualTo(4));
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void PrepareRun_MissingProfileIsRejected()
        {
            Assert.That(MoralePyramidSystem.TryPrepareRun(null, out string error), Is.False);
            Assert.That(error, Does.Contain("not assigned"));
            Assert.That(MoralePyramidSystem.IsInitialized, Is.False);
            Assert.That(EnergySystem.ProfileMaximumEnergyModifier, Is.Zero);
        }

        [Test]
        public void StableStage_HasNoRoundEndOrEnergyEffect()
        {
            MoralePyramidProfileData profile = PrepareDemoProfile();
            try
            {
                ResourceSystem.Reset(3);
                EnergySystem.Reset(3);

                ValueConsumptionResult loss =
                    MoralePyramidSystem.ResolveRoundEndResourceLoss(1);
                bool committed = MoralePyramidSystem.CommitPendingTurnEffects();

                Assert.That(loss.Requested, Is.Zero);
                Assert.That(ResourceSystem.Resource, Is.EqualTo(3));
                Assert.That(committed, Is.False);
                Assert.That(EnergySystem.MaxEnergy, Is.EqualTo(3));
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void ShakenStage_LosesOneResourceOncePerRoundAndFloorsAtZero()
        {
            MoralePyramidProfileData profile = PrepareDemoProfile();
            try
            {
                ResourceSystem.Reset(1);
                MoraleSystem.CommitChange("EnterShaken", -31);

                ValueConsumptionResult first =
                    MoralePyramidSystem.ResolveRoundEndResourceLoss(1);
                ValueConsumptionResult duplicate =
                    MoralePyramidSystem.ResolveRoundEndResourceLoss(1);
                ValueConsumptionResult nextRound =
                    MoralePyramidSystem.ResolveRoundEndResourceLoss(2);

                Assert.That(first.Requested, Is.EqualTo(1));
                Assert.That(first.Consumed, Is.EqualTo(1));
                Assert.That(duplicate.Requested, Is.Zero);
                Assert.That(nextRound.Requested, Is.EqualTo(1));
                Assert.That(nextRound.Consumed, Is.Zero);
                Assert.That(ResourceSystem.Resource, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void ShakenEnteredDuringRoundEnd_DoesNotRetroactivelyChargeThatRound()
        {
            GameObject owner = new GameObject("MoralePyramidRoundEndTest");
            RoundEndResolver resolver = owner.AddComponent<RoundEndResolver>();
            MoralePyramidProfileData profile = MoralePyramidTestFactory.CreateDemoProfile();
            try
            {
                resolver.PrepareRun();
                Assert.That(MoralePyramidSystem.TryPrepareRun(profile, out string error), Is.True, error);
                MoraleSystem.Reset(startingMorale: 75);
                ResourceSystem.Reset(2);
                FoodSystem.Reset(0);

                RoundEndReport first = resolver.ResolveRoundEnd(null, 1, 0);
                RoundEndReport second = resolver.ResolveRoundEnd(null, 2, 0);

                Assert.That(first.MoraleStageBefore, Is.EqualTo(MoraleCollapseStage.Stable));
                Assert.That(first.MoraleStageAfter, Is.EqualTo(MoraleCollapseStage.Shaken));
                Assert.That(first.MoraleStageResourceLossApplied, Is.Zero);
                Assert.That(second.MoraleStageResourceLossApplied, Is.EqualTo(1));
                Assert.That(ResourceSystem.Resource, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(owner);
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void BrinkStage_ReplacesShakenResourceLoss()
        {
            MoralePyramidProfileData profile = PrepareDemoProfile();
            try
            {
                ResourceSystem.Reset(3);
                MoraleSystem.CommitChange("EnterBrink", -66);

                ValueConsumptionResult loss =
                    MoralePyramidSystem.ResolveRoundEndResourceLoss(1);

                Assert.That(MoralePyramidSystem.ActiveCollapseStage, Is.EqualTo(MoraleCollapseStage.Brink));
                Assert.That(loss.Requested, Is.Zero);
                Assert.That(ResourceSystem.Resource, Is.EqualTo(3));
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void BrinkEnergyModifier_WaitsForTurnBoundaryAndAllowsZeroMaximum()
        {
            EnergySystem.Reset(1);
            MoralePyramidProfileData profile = PrepareDemoProfile();
            try
            {
                MoraleSystem.CommitChange("EnterBrink", -66);

                Assert.That(MoralePyramidSystem.HasPendingMaximumEnergyModifier, Is.True);
                Assert.That(EnergySystem.MaxEnergy, Is.EqualTo(1));
                Assert.That(EnergySystem.CurrentEnergy, Is.EqualTo(1));

                Assert.That(MoralePyramidSystem.CommitPendingTurnEffects(), Is.True);
                EnergySystem.RefillToMax();

                Assert.That(EnergySystem.BaseMaxEnergy, Is.EqualTo(1));
                Assert.That(EnergySystem.ProfileMaximumEnergyModifier, Is.EqualTo(-1));
                Assert.That(EnergySystem.MaxEnergy, Is.Zero);
                Assert.That(EnergySystem.CurrentEnergy, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void BrinkEnergyModifier_DoesNotStackAndPreservesLaterBaseGrowth()
        {
            EnergySystem.Reset(1);
            MoralePyramidProfileData profile = PrepareDemoProfile();
            try
            {
                MoraleSystem.CommitChange("EnterBrink", -66);
                MoralePyramidSystem.CommitPendingTurnEffects();
                Assert.That(MoralePyramidSystem.CommitPendingTurnEffects(), Is.False);

                EnergySystem.IncreaseMaxEnergy(2);

                Assert.That(EnergySystem.BaseMaxEnergy, Is.EqualTo(3));
                Assert.That(EnergySystem.MaxEnergy, Is.EqualTo(2));
                Assert.That(EnergySystem.ProfileMaximumEnergyModifier, Is.EqualTo(-1));
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void MoraleRecovery_DoesNotRestoreEarlierStageEffects()
        {
            MoralePyramidProfileData profile = PrepareDemoProfile();
            try
            {
                ResourceSystem.Reset(3);
                MoraleSystem.CommitChange("EnterBrink", -66);
                MoraleSystem.CommitChange("Recover", 100);

                ValueConsumptionResult loss =
                    MoralePyramidSystem.ResolveRoundEndResourceLoss(1);

                Assert.That(MoraleSystem.CurrentMorale, Is.EqualTo(100));
                Assert.That(MoralePyramidSystem.ActiveCollapseStage, Is.EqualTo(MoraleCollapseStage.Brink));
                Assert.That(loss.Requested, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void DirectStableToBrink_ActivatesOnlyFinalStageOnce()
        {
            MoralePyramidProfileData profile = PrepareDemoProfile();
            int eventCount = 0;
            void HandleStage(MoraleStageSnapshot _) => eventCount++;
            MoralePyramidSystem.ActiveStageChanged += HandleStage;
            try
            {
                MoraleSystem.CommitChange("MajorCrisis", -66);

                Assert.That(MoralePyramidSystem.ActiveCollapseStage, Is.EqualTo(MoraleCollapseStage.Brink));
                Assert.That(MoralePyramidSystem.ActiveStage.ResourceLossPerRound, Is.Zero);
                Assert.That(MoralePyramidSystem.ActiveStage.MaximumEnergyModifier, Is.EqualTo(-1));
                Assert.That(eventCount, Is.EqualTo(1));
            }
            finally
            {
                MoralePyramidSystem.ActiveStageChanged -= HandleStage;
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void Reset_ClearsSnapshotPendingModifierAndRoundExecutionRecord()
        {
            EnergySystem.Reset(1);
            MoralePyramidProfileData profile = PrepareDemoProfile();
            try
            {
                MoraleSystem.CommitChange("EnterBrink", -66);
                Assert.That(MoralePyramidSystem.HasPendingMaximumEnergyModifier, Is.True);
                MoralePyramidSystem.CommitPendingTurnEffects();
                Assert.That(EnergySystem.ProfileMaximumEnergyModifier, Is.EqualTo(-1));

                MoralePyramidSystem.Reset();

                Assert.That(MoralePyramidSystem.IsInitialized, Is.False);
                Assert.That(MoralePyramidSystem.HasPendingMaximumEnergyModifier, Is.False);
                Assert.That(EnergySystem.ProfileMaximumEnergyModifier, Is.Zero);

                MoraleSystem.Reset();
                ResourceSystem.Reset(2);
                Assert.That(MoralePyramidSystem.TryPrepareRun(profile, out string firstError), Is.True, firstError);
                MoraleSystem.CommitChange("FirstRunShaken", -31);
                Assert.That(
                    MoralePyramidSystem.ResolveRoundEndResourceLoss(1).Consumed,
                    Is.EqualTo(1));

                MoralePyramidSystem.Reset();
                MoraleSystem.Reset();
                Assert.That(MoralePyramidSystem.TryPrepareRun(profile, out string secondError), Is.True, secondError);
                MoraleSystem.CommitChange("SecondRunShaken", -31);
                Assert.That(
                    MoralePyramidSystem.ResolveRoundEndResourceLoss(1).Consumed,
                    Is.EqualTo(1));
                Assert.That(ResourceSystem.Resource, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void RunStartWithoutProfile_IsBlocked()
        {
            GameObject owner = new GameObject("MissingMoraleProfileRunTest");
            owner.SetActive(false);
            RunPhaseController controller = owner.AddComponent<RunPhaseController>();
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("startRunOnAwake").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            owner.SetActive(true);

            try
            {
                LogAssert.Expect(LogType.Error, "Run start blocked: Morale Pyramid Profile is not assigned.");
                controller.StartRun();

                Assert.That(controller.IsRunEnded, Is.True);
                Assert.That(MoralePyramidSystem.IsInitialized, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        private static MoralePyramidProfileData PrepareDemoProfile()
        {
            MoralePyramidProfileData profile = MoralePyramidTestFactory.CreateDemoProfile();
            Assert.That(MoralePyramidSystem.TryPrepareRun(profile, out string error), Is.True, error);
            return profile;
        }
    }
}
