using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ZombieCardSurvive.Run;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Tests.Editor
{
    public sealed class MoraleCollapseCoreTests
    {
        [SetUp]
        public void SetUp()
        {
            MoraleSystem.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            MoraleSystem.Reset();
        }

        [Test]
        public void Reset_UsesNewRunDefaults()
        {
            Assert.That(MoraleSystem.CurrentMorale, Is.EqualTo(100));
            Assert.That(MoraleSystem.MaximumMorale, Is.EqualTo(100));
            Assert.That(MoraleSystem.CollapseStage, Is.EqualTo(MoraleCollapseStage.Stable));
            Assert.That(MoraleSystem.IsCollapsed, Is.False);
        }

        [Test]
        public void StableThreshold_SeventyStaysStable_AndSixtyNineEntersShakenOnce()
        {
            int stageEventCount = 0;
            void HandleStage(MoraleStageTransition _, MoraleChangeResult __) => stageEventCount++;
            MoraleSystem.StageChanged += HandleStage;

            try
            {
                MoraleChangeResult atThreshold = MoraleSystem.CommitChange("At70", -30);
                MoraleChangeResult belowThreshold = MoraleSystem.CommitChange("At69", -1);
                MoraleChangeResult repeated = MoraleSystem.CommitChange("StillBelow70", -1);

                Assert.That(atThreshold.StageAfter, Is.EqualTo(MoraleCollapseStage.Stable));
                Assert.That(atThreshold.StageTransitions, Is.Empty);
                Assert.That(belowThreshold.StageAfter, Is.EqualTo(MoraleCollapseStage.Shaken));
                Assert.That(belowThreshold.StageTransitions.Count, Is.EqualTo(1));
                Assert.That(repeated.StageTransitions, Is.Empty);
                Assert.That(stageEventCount, Is.EqualTo(1));
            }
            finally
            {
                MoraleSystem.StageChanged -= HandleStage;
            }
        }

        [Test]
        public void CriticalThreshold_ThirtyFiveStaysShaken_AndThirtyFourEntersBrink()
        {
            MoraleSystem.CommitChange("EnterShaken", -31);

            MoraleChangeResult atThreshold = MoraleSystem.CommitChange("At35", -34);
            MoraleChangeResult belowThreshold = MoraleSystem.CommitChange("At34", -1);

            Assert.That(atThreshold.MoraleAfter, Is.EqualTo(35));
            Assert.That(atThreshold.StageAfter, Is.EqualTo(MoraleCollapseStage.Shaken));
            Assert.That(belowThreshold.MoraleAfter, Is.EqualTo(34));
            Assert.That(belowThreshold.StageAfter, Is.EqualTo(MoraleCollapseStage.Brink));
            Assert.That(belowThreshold.StageTransitions.Count, Is.EqualTo(1));
        }

        [Test]
        public void SingleBatch_CanCrossBothStages_AndRecordsBothTransitions()
        {
            MoraleSystem.Reset(80);

            MoraleChangeResult result = MoraleSystem.CommitChange("MajorCrisis", -50);

            Assert.That(result.MoraleAfter, Is.EqualTo(30));
            Assert.That(result.StageAfter, Is.EqualTo(MoraleCollapseStage.Brink));
            Assert.That(result.StageTransitions.Count, Is.EqualTo(2));
            Assert.That(result.StageTransitions[0].From, Is.EqualTo(MoraleCollapseStage.Stable));
            Assert.That(result.StageTransitions[0].To, Is.EqualTo(MoraleCollapseStage.Shaken));
            Assert.That(result.StageTransitions[1].From, Is.EqualTo(MoraleCollapseStage.Shaken));
            Assert.That(result.StageTransitions[1].To, Is.EqualTo(MoraleCollapseStage.Brink));
        }

        [Test]
        public void Recovery_DoesNotRestorePreviousStage()
        {
            MoraleSystem.CommitChange("Crisis", -40);

            MoraleChangeResult recovery = MoraleSystem.CommitChange("Recovery", 100);

            Assert.That(recovery.MoraleAfter, Is.EqualTo(100));
            Assert.That(recovery.StageAfter, Is.EqualTo(MoraleCollapseStage.Shaken));
            Assert.That(recovery.StageTransitions, Is.Empty);
        }

        [Test]
        public void MaximumMoraleChange_ClampsBeforeCurrentDelta_WithoutDuplicateDamage()
        {
            MoraleSystem.Reset(90);

            MoraleChangeResult result = MoraleSystem.CommitChange(
                "PermanentTrauma",
                moraleDelta: -5,
                maximumMoraleDelta: -20);

            Assert.That(result.MaximumMoraleAfter, Is.EqualTo(80));
            Assert.That(result.MoraleAfter, Is.EqualTo(75));
            Assert.That(result.AppliedMaximumMoraleDelta, Is.EqualTo(-20));
            Assert.That(result.AppliedMoraleDelta, Is.EqualTo(-15));

            MoraleSystem.AddMorale(100);
            Assert.That(MoraleSystem.CurrentMorale, Is.EqualTo(80));
        }

        [Test]
        public void MaximumMoraleClamp_CanAdvanceIrreversibleStage()
        {
            MoraleChangeResult result = MoraleSystem.CommitChange(
                "SevereTrauma",
                maximumMoraleDelta: -70);

            Assert.That(result.MaximumMoraleAfter, Is.EqualTo(30));
            Assert.That(result.MoraleAfter, Is.EqualTo(30));
            Assert.That(result.StageAfter, Is.EqualTo(MoraleCollapseStage.Brink));
            Assert.That(result.StageTransitions.Count, Is.EqualTo(2));
        }

        [Test]
        public void Collapse_FiresOnce_AndRejectsLaterChanges()
        {
            int collapseCount = 0;
            void HandleCollapse(MoraleChangeResult _) => collapseCount++;
            MoraleSystem.BaseCollapsed += HandleCollapse;

            try
            {
                MoraleChangeResult first = MoraleSystem.CommitChange("FinalLoss", -100);
                MoraleChangeResult second = MoraleSystem.CommitChange("RepeatedLoss", -1);
                MoraleChangeResult recovery = MoraleSystem.CommitChange("TooLate", 100);

                Assert.That(first.DidCollapse, Is.True);
                Assert.That(second.DidCollapse, Is.False);
                Assert.That(recovery.MoraleAfter, Is.Zero);
                Assert.That(MoraleSystem.IsCollapsed, Is.True);
                Assert.That(collapseCount, Is.EqualTo(1));
            }
            finally
            {
                MoraleSystem.BaseCollapsed -= HandleCollapse;
            }
        }

        [Test]
        public void LegacyMoraleMethods_UseTheNewCommitPipeline()
        {
            MoraleSystem.ReduceMorale(31);
            MoraleSystem.AddMorale(10);
            MoraleSystem.ChangeMorale(-5);

            Assert.That(MoraleSystem.CurrentMorale, Is.EqualTo(74));
            Assert.That(MoraleSystem.CollapseStage, Is.EqualTo(MoraleCollapseStage.Shaken));
        }

        [Test]
        public void RunPhaseController_CollapseStopsProgression_AndStartRunResetsState()
        {
            GameObject owner = new GameObject("MoraleCollapseRunPhaseTest");
            owner.SetActive(false);
            RunPhaseController controller = owner.AddComponent<RunPhaseController>();
            MoralePyramidProfileData profile = MoralePyramidTestFactory.CreateDemoProfile();
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("startRunOnAwake").boolValue = false;
            serialized.FindProperty("defaultMoraleProfile").objectReferenceValue = profile;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            InvokePrivate(controller, "Awake");
            ClearSceneReferences(controller);

            int failureCount = 0;
            controller.RunFailed += _ => failureCount++;

            try
            {
                MoraleSystem.CommitChange("FinalLoss", -100);
                controller.EnterTurnPhase();
                controller.EnterEventPhase();

                Assert.That(controller.IsRunEnded, Is.True);
                Assert.That(controller.CurrentPhase, Is.EqualTo(RunPhase.Failed));
                Assert.That(controller.TurnIndex, Is.Zero);
                Assert.That(failureCount, Is.EqualTo(1));

                controller.StartRun();

                Assert.That(controller.IsRunEnded, Is.False);
                Assert.That(controller.CurrentPhase, Is.EqualTo(RunPhase.Event));
                Assert.That(MoraleSystem.CurrentMorale, Is.EqualTo(100));
                Assert.That(MoraleSystem.MaximumMorale, Is.EqualTo(100));
                Assert.That(MoraleSystem.CollapseStage, Is.EqualTo(MoraleCollapseStage.Stable));
            }
            finally
            {
                Object.DestroyImmediate(owner);
                Object.DestroyImmediate(profile);
            }
        }

        private static void ClearSceneReferences(RunPhaseController controller)
        {
            string[] fieldNames =
            {
                "cardController",
                "deckReplacementView",
                "roundEndResolver",
                "buildingSystem",
                "explorationSystem",
                "eventFlowController"
            };

            foreach (string fieldName in fieldNames)
            {
                FieldInfo field = typeof(RunPhaseController).GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic);
                field?.SetValue(controller, null);
            }
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing private method {methodName}.");
            method.Invoke(target, null);
        }
    }
}
