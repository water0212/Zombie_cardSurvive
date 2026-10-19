using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ZombieCardSurvive.Buildings.Runtime;
using ZombieCardSurvive.Cards.Data;
using ZombieCardSurvive.Effects.Data;
using ZombieCardSurvive.Events.Data;
using ZombieCardSurvive.Events.Editor;
using ZombieCardSurvive.Events.Runtime;
using ZombieCardSurvive.Systems;
using ZombieCardSurvive.Threats.Data;
using ZombieCardSurvive.Threats.Editor;
using ZombieCardSurvive.Threats.Runtime;

namespace ZombieCardSurvive.Tests.Editor
{
    public sealed class EventHordeContentV1Tests
    {
        private readonly List<UnityEngine.Object> createdObjects = new List<UnityEngine.Object>();

        [SetUp]
        public void SetUp()
        {
            ResourceReservationService.Reset();
            FoodSystem.Reset(100);
            ResourceSystem.Reset(100);
            CombatSystem.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            CombatSystem.Reset();
            ResourceReservationService.Reset();
            foreach (UnityEngine.Object createdObject in createdObjects)
            {
                if (createdObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(createdObject);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void FormalHordeAssetsMatchTheApprovedV1Specification()
        {
            Dictionary<string, ZombieCardData> zombies = HordeContentV1SetupUtility.LoadZombieAssets();
            Dictionary<string, HordeProfileData> profiles = HordeContentV1SetupUtility.LoadProfileAssets();
            List<string> errors = new List<string>();

            HordeContentV1SetupUtility.ValidateContent(zombies, profiles, errors);

            Assert.That(errors, Is.Empty, string.Join("\n", errors));
            Assert.That(zombies.Keys, Is.EquivalentTo(new[] { "ZMB-001", "ZMB-002", "ZMB-003" }));
            Assert.That(profiles.Keys, Is.EquivalentTo(new[] { HordeContentV1Specification.StandardProfileId }));
        }

        [Test]
        public void StandardProfileUsesOneBudgetAndKeepsSpawnCountWithinUiLimit()
        {
            HordeProfileData profile = LoadStandardProfile();
            HordeBudgetRuntime budgetRuntime = new HordeBudgetRuntime();
            HordeCompositionRuntime compositionRuntime = new HordeCompositionRuntime();

            for (int tier = 1; tier <= 5; tier++)
            {
                HordeBudgetSnapshot budget = budgetRuntime.Capture(
                    profile,
                    new HordeBudgetInputSnapshot(tier));
                Assert.That(budget.FinalBudget, Is.EqualTo(Math.Min(10, 3 + (tier - 1) * 2)));

                for (int seed = 0; seed < 100; seed++)
                {
                    HordeCompositionResult composition = compositionRuntime.Generate(
                        profile,
                        budget,
                        new System.Random(seed));

                    Assert.That(composition.SpawnCount, Is.InRange(1, 8), $"Tier {tier}, seed {seed}");
                    Assert.That(composition.SpentBudget, Is.LessThanOrEqualTo(budget.FinalBudget));
                    Assert.That(
                        composition.Entries.All(entry =>
                            entry.SourceEntry.IsAllowedAtWorldThreatTier(tier)),
                        Is.True,
                        $"Tier {tier}, seed {seed}");
                }
            }

            HordeCompositionResult tierOne = compositionRuntime.Generate(
                profile,
                budgetRuntime.Capture(profile, new HordeBudgetInputSnapshot(1)),
                new System.Random(9));
            Assert.That(tierOne.SpawnCount, Is.EqualTo(3));
            Assert.That(tierOne.Entries.All(entry => entry.ZombieCard.CardId == "ZMB-001"), Is.True);
        }

        [Test]
        public void EventSpecificationsSetPendingFlagThenSpawnBeforeClearingIt()
        {
            EventContentSpec warning = EventContentV1Specification.Events
                .Single(spec => spec.EventId == "EVT-008");
            EventContentSpec arrival = EventContentV1Specification.Events
                .Single(spec => spec.EventId == "EVT-014");

            Assert.That(warning.IncludeInPlayablePool, Is.True);
            Assert.That(warning.Choices.Count, Is.EqualTo(3));
            Assert.That(
                warning.Choices.All(choice => choice.Effects.Any(effect =>
                    effect.Category == GameEffectCategory.EventState
                    && effect.EventStateType == EventStateEffectType.SetRunFlagValue
                    && effect.TargetId == EventContentV1Specification.HordePendingFlagId
                    && effect.Amount == 1)),
                Is.True);

            Assert.That(arrival.IncludeInPlayablePool, Is.True);
            Assert.That(arrival.Choices.Single().Effects.Count, Is.EqualTo(2));
            Assert.That(arrival.Choices.Single().Effects[0].Category, Is.EqualTo(GameEffectCategory.Threat));
            Assert.That(arrival.Choices.Single().Effects[0].ThreatType, Is.EqualTo(ThreatEffectType.SpawnHorde));
            Assert.That(
                arrival.Choices.Single().Effects[0].HordeProfileId,
                Is.EqualTo(HordeContentV1Specification.StandardProfileId));
            Assert.That(arrival.Choices.Single().Effects[1].Category, Is.EqualTo(GameEffectCategory.EventState));
            Assert.That(
                arrival.Choices.Single().Effects[1].EventStateType,
                Is.EqualTo(EventStateEffectType.ClearRunFlag));
        }

        [Test]
        public void ImportedEventsReferenceFormalProfileAndRunFlagAssets()
        {
            EventData warning = LoadEvent("EVT-008");
            EventData arrival = LoadEvent("EVT-014");
            HordeProfileData profile = LoadStandardProfile();

            Assert.That(warning.IsImplementationReady, Is.True);
            Assert.That(warning.Choices.All(choice => choice.Effects.Any(effect =>
                effect.Category == GameEffectCategory.EventState
                && effect.RunFlagId == EventContentV1Specification.HordePendingFlagId)), Is.True);

            Assert.That(arrival.IsImplementationReady, Is.True);
            Assert.That(arrival.Choices.Single().Effects[0].IsSpawnHorde, Is.True);
            Assert.That(arrival.Choices.Single().Effects[0].HordeProfile, Is.SameAs(profile));
            Assert.That(
                arrival.Choices.Single().Effects[1].EventStateEffectType,
                Is.EqualTo(EventStateEffectType.ClearRunFlag));
        }

        [Test]
        public void FormalEventChainSpawnsTierOneHordeAndClearsPendingFlag()
        {
            EventData warning = LoadEvent("EVT-008");
            EventData arrival = LoadEvent("EVT-014");
            GameObject owner = new GameObject("FormalEventHordeChainTest");
            createdObjects.Add(owner);
            EventFlowController flow = owner.AddComponent<EventFlowController>();
            flow.SetEventSequenceSource(new FixedEventSequence(warning, arrival));
            flow.InitializeRun(14014);
            flow.SetThreatScaleTiers(1, 1);

            flow.BeginEventPhase(8);
            Assert.That(flow.CurrentEvent, Is.SameAs(warning));
            Assert.That(flow.ChooseOption(2), Is.True);
            Assert.That(flow.HasRunFlag(EventContentV1Specification.HordePendingFlagId), Is.True);
            Assert.That(CombatSystem.ZombieThreatCount, Is.Zero);

            flow.BeginEventPhase(11);
            Assert.That(flow.CurrentEvent, Is.SameAs(arrival));
            Assert.That(flow.CurrentResolvedEvent.Choices[0].ResultDescription, Does.Contain("3 隻"));
            Assert.That(flow.ChooseOption(0), Is.True);

            Assert.That(CombatSystem.ZombieThreatCount, Is.EqualTo(3));
            Assert.That(
                CombatSystem.ZombieThreats.All(threat => threat.SourceCard.CardId == "ZMB-001"),
                Is.True);
            Assert.That(flow.HasRunFlag(EventContentV1Specification.HordePendingFlagId), Is.False);
        }

        private static HordeProfileData LoadStandardProfile()
        {
            HordeProfileData profile = AssetDatabase.LoadAssetAtPath<HordeProfileData>(
                HordeContentV1SetupUtility.GetProfileAssetPath(
                    HordeContentV1Specification.StandardProfileId));
            Assert.That(profile, Is.Not.Null, "Run Setup Event Content V1 before this content test.");
            return profile;
        }

        private static EventData LoadEvent(string eventId)
        {
            EventData result = AssetDatabase.FindAssets("t:EventData")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<EventData>)
                .SingleOrDefault(eventData => eventData != null && eventData.EventId == eventId);
            Assert.That(result, Is.Not.Null, $"Run Setup Event Content V1 before testing {eventId}.");
            return result;
        }

        private sealed class FixedEventSequence : IEventSequenceSource
        {
            private readonly Queue<EventData> events;

            public FixedEventSequence(params EventData[] events)
            {
                this.events = new Queue<EventData>(events ?? Array.Empty<EventData>());
            }

            public bool IsActive => events.Count > 0;

            public bool TryGetNextEvent(int day, out EventData eventData)
            {
                eventData = events.Count > 0 ? events.Dequeue() : null;
                return eventData != null;
            }
        }
    }
}
