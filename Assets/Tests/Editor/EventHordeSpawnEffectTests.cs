using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ZombieCardSurvive.Buildings.Runtime;
using ZombieCardSurvive.Cards.Data;
using ZombieCardSurvive.Effects.Data;
using ZombieCardSurvive.Events.Data;
using ZombieCardSurvive.Events.Runtime;
using ZombieCardSurvive.Systems;
using ZombieCardSurvive.Threats.Data;
using ZombieCardSurvive.Threats.Runtime;

namespace ZombieCardSurvive.Tests.Editor
{
    public sealed class EventHordeSpawnEffectTests
    {
        private readonly List<UnityEngine.Object> createdObjects = new List<UnityEngine.Object>();

        [SetUp]
        public void SetUp()
        {
            ResourceReservationService.Reset();
            FoodSystem.Reset(0);
            ResourceSystem.Reset(0);
            CombatSystem.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            ResourceReservationService.Reset();
            CombatSystem.Reset();
            for (int index = createdObjects.Count - 1; index >= 0; index--)
            {
                if (createdObjects[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(createdObjects[index]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void ResolvedSpawnHordeLocksBudgetAndCompositionWithoutSpawning()
        {
            ZombieCardData weak = CreateZombie("Weak", health: 1, attack: 1);
            HordeProfileData profile = CreateProfile(
                HordeCompositionStyle.FixedOrder,
                baseBudget: 1,
                worldThreatBudgetPerTier: 2,
                baseAttentionBudgetPerPoint: 2f,
                entries: new[] { new ArchetypeSetup(weak, cost: 1) });
            GameEffectData spawn = CreateSpawnHordeEffect(profile, eventBonusBudget: 2);
            ThreatSnapshot threat = CreateThreat(worldThreatTier: 3, baseAttention: 1.5f);

            ResolvedEventChoiceRuntime choice = ResolveChoice(
                new[] { spawn },
                threat,
                runSeed: 123,
                eventDay: 7,
                eventId: "EVENT-HORDE-LOCK");
            ResolvedGameEffectRuntime resolved = choice.Effects[0];

            Assert.That(resolved.HordeSpawn, Is.Not.Null);
            Assert.That(resolved.HordeSpawn.BudgetSnapshot.FinalBudget, Is.EqualTo(10));
            Assert.That(resolved.HordeSpawn.BudgetSnapshot.Input.WorldThreatTier, Is.EqualTo(3));
            Assert.That(resolved.HordeSpawn.BudgetSnapshot.Input.BaseAttention, Is.EqualTo(1.5f).Within(0.0001f));
            Assert.That(resolved.HordeSpawn.BudgetSnapshot.Input.RegionPressure, Is.Zero);
            Assert.That(resolved.HordeSpawn.BudgetSnapshot.Input.EventBonusBudget, Is.EqualTo(2));
            Assert.That(resolved.HordeSpawn.CompositionResult.SpawnCount, Is.EqualTo(10));
            Assert.That(resolved.FinalAmount, Is.EqualTo(10));
            Assert.That(CombatSystem.ZombieThreatCount, Is.Zero);
        }

        [Test]
        public void EventFlowUsesLockedThreatAndSpawnsOnlyAfterChoice()
        {
            ZombieCardData weak = CreateZombie("Weak", health: 1, attack: 1);
            HordeProfileData profile = CreateProfile(
                HordeCompositionStyle.FixedOrder,
                baseBudget: 2,
                worldThreatBudgetPerTier: 1,
                baseAttentionBudgetPerPoint: 1f,
                entries: new[] { new ArchetypeSetup(weak, cost: 1) });
            EventData eventData = CreateEvent(
                "EVENT-HORDE-FLOW",
                CreateChoiceData(new[] { CreateSpawnHordeEffect(profile) }));
            GameObject owner = new GameObject("EventHordeFlowTest");
            createdObjects.Add(owner);
            EventFlowController flow = owner.AddComponent<EventFlowController>();
            SetField(flow, "eventPool", new List<EventData> { eventData });
            flow.InitializeRun(2468);
            flow.SetThreatScaleTiers(worldThreat: 3, hordeBudget: 1);
            flow.SetBaseAttentionModifiers(2f, 0f, 0f);

            flow.BeginEventPhase(10);
            ResolvedHordeSpawnRuntime locked = flow.CurrentResolvedEvent.Choices[0].Effects[0].HordeSpawn;
            flow.SetThreatScaleTiers(worldThreat: 8, hordeBudget: 1);
            flow.SetBaseAttentionModifiers(20f, 0f, 0f);

            Assert.That(CombatSystem.ZombieThreatCount, Is.Zero);
            Assert.That(locked.BudgetSnapshot.FinalBudget, Is.EqualTo(6));
            Assert.That(locked.BudgetSnapshot.Input.WorldThreatTier, Is.EqualTo(3));
            Assert.That(locked.BudgetSnapshot.Input.BaseAttention, Is.EqualTo(2f).Within(0.0001f));

            bool selected = flow.ChooseOption(0);

            Assert.That(selected, Is.True);
            Assert.That(CombatSystem.ZombieThreatCount, Is.EqualTo(6));
            Assert.That(locked.HasExecuted, Is.True);
            Assert.That(locked.SpawnedThreats.Count, Is.EqualTo(6));
            foreach (ZombieThreatRuntime threat in locked.SpawnedThreats)
            {
                Assert.That(threat.SourceCard, Is.SameAs(weak));
            }
        }

        [Test]
        public void SameEventInputsProduceStableSeedAndComposition()
        {
            ZombieCardData weak = CreateZombie("Weak", health: 1, attack: 1);
            ZombieCardData brute = CreateZombie("Brute", health: 3, attack: 2);
            HordeProfileData profile = CreateProfile(
                HordeCompositionStyle.Mixed,
                baseBudget: 12,
                entries: new[]
                {
                    new ArchetypeSetup(weak, cost: 1, weight: 10),
                    new ArchetypeSetup(brute, cost: 3, weight: 10)
                });
            GameEffectData spawn = CreateSpawnHordeEffect(profile);
            ThreatSnapshot threat = CreateThreat(worldThreatTier: 2, baseAttention: 0f);

            ResolvedHordeSpawnRuntime first = ResolveChoice(
                new[] { spawn },
                threat,
                runSeed: 99,
                eventDay: 12,
                eventId: "EVENT-STABLE").Effects[0].HordeSpawn;
            ResolvedHordeSpawnRuntime second = ResolveChoice(
                new[] { spawn },
                threat,
                runSeed: 99,
                eventDay: 12,
                eventId: "EVENT-STABLE").Effects[0].HordeSpawn;

            Assert.That(second.Seed, Is.EqualTo(first.Seed));
            Assert.That(second.CompositionResult.SpawnCount, Is.EqualTo(first.CompositionResult.SpawnCount));
            for (int index = 0; index < first.CompositionResult.SpawnCount; index++)
            {
                Assert.That(
                    second.CompositionResult.Entries[index].ZombieCard,
                    Is.SameAs(first.CompositionResult.Entries[index].ZombieCard));
            }
        }

        [Test]
        public void MissingProfileBlocksChoiceWithoutSpendingOrClearingFlag()
        {
            ResourceSystem.Reset(10);
            RunFlagRuntimeStore flags = new RunFlagRuntimeStore();
            flags.SetValue("FLAG-HORDE-PENDING", 1, day: 1);
            ResolvedEventChoiceRuntime choice = ResolveChoice(
                new[]
                {
                    CreateResourceCost(5),
                    CreateSpawnHordeEffect(null),
                    CreateClearFlagEffect("FLAG-HORDE-PENDING")
                },
                CreateThreat(1, 0f));
            GameEffectContext context = GameEffectContext.ForEvent(flags, eventDay: 1);

            EventChoiceAvailability availability = choice.EvaluateAvailability(context);
            bool succeeded = choice.TryExecute(context, out EventChoiceExecutionResult result);

            Assert.That(availability.IsAvailable, Is.False);
            Assert.That(availability.BlockReason, Is.EqualTo(EventChoiceBlockReason.InvalidConfiguration));
            Assert.That(succeeded, Is.False);
            Assert.That(result.Succeeded, Is.False);
            Assert.That(ResourceSystem.Resource, Is.EqualTo(10));
            Assert.That(flags.IsActive("FLAG-HORDE-PENDING"), Is.True);
            Assert.That(CombatSystem.ZombieThreatCount, Is.Zero);
        }

        [Test]
        public void BudgetTooLowBlocksChoiceWithoutPartialEffects()
        {
            ZombieCardData brute = CreateZombie("Brute", health: 3, attack: 2);
            HordeProfileData profile = CreateProfile(
                HordeCompositionStyle.FixedOrder,
                baseBudget: 1,
                entries: new[] { new ArchetypeSetup(brute, cost: 2) });
            ResourceSystem.Reset(10);
            RunFlagRuntimeStore flags = new RunFlagRuntimeStore();
            flags.SetValue("FLAG-HORDE-PENDING", 1, day: 1);
            ResolvedEventChoiceRuntime choice = ResolveChoice(
                new[]
                {
                    CreateResourceCost(5),
                    CreateSpawnHordeEffect(profile),
                    CreateClearFlagEffect("FLAG-HORDE-PENDING")
                },
                CreateThreat(1, 0f));
            GameEffectContext context = GameEffectContext.ForEvent(flags, eventDay: 1);

            EventChoiceAvailability availability = choice.EvaluateAvailability(context);
            bool succeeded = choice.TryExecute(context, out _);

            Assert.That(availability.IsAvailable, Is.False);
            Assert.That(availability.BlockReason, Is.EqualTo(EventChoiceBlockReason.EffectUnavailable));
            Assert.That(succeeded, Is.False);
            Assert.That(ResourceSystem.Resource, Is.EqualTo(10));
            Assert.That(flags.IsActive("FLAG-HORDE-PENDING"), Is.True);
            Assert.That(CombatSystem.ZombieThreatCount, Is.Zero);
        }

        [Test]
        public void BatchSpawnAppendsThreatsAndNotifiesOnce()
        {
            ZombieCardData existing = CreateZombie("Existing", health: 1, attack: 1);
            ZombieCardData weak = CreateZombie("Weak", health: 1, attack: 1);
            ZombieCardData brute = CreateZombie("Brute", health: 3, attack: 2);
            CombatSystem.SpawnZombieThreat(existing);
            int notifications = 0;
            void CountNotification() => notifications++;
            CombatSystem.StateChanged += CountNotification;

            try
            {
                IReadOnlyList<ZombieThreatRuntime> spawned = CombatSystem.SpawnZombieThreats(
                    new[] { weak, brute });

                Assert.That(spawned.Count, Is.EqualTo(2));
                Assert.That(CombatSystem.ZombieThreatCount, Is.EqualTo(3));
                Assert.That(CombatSystem.ZombieThreats[0].SourceCard, Is.SameAs(existing));
                Assert.That(spawned[0].SourceCard, Is.SameAs(weak));
                Assert.That(spawned[1].SourceCard, Is.SameAs(brute));
                Assert.That(notifications, Is.EqualTo(1));

                IReadOnlyList<ZombieThreatRuntime> invalid = CombatSystem.SpawnZombieThreats(
                    new ZombieCardData[] { weak, null });
                Assert.That(invalid, Is.Empty);
                Assert.That(CombatSystem.ZombieThreatCount, Is.EqualTo(3));
                Assert.That(notifications, Is.EqualTo(1));
            }
            finally
            {
                CombatSystem.StateChanged -= CountNotification;
            }
        }

        [Test]
        public void ResolvedHordeCanExecuteOnlyOnce()
        {
            ZombieCardData weak = CreateZombie("Weak", health: 1, attack: 1);
            HordeProfileData profile = CreateProfile(
                HordeCompositionStyle.FixedOrder,
                baseBudget: 2,
                entries: new[] { new ArchetypeSetup(weak, cost: 1) });
            ResolvedEventChoiceRuntime choice = ResolveChoice(
                new[] { CreateSpawnHordeEffect(profile) },
                CreateThreat(1, 0f));

            bool first = choice.TryExecute(GameEffectContext.ForEvent(), out _);
            bool second = choice.TryExecute(GameEffectContext.ForEvent(), out _);

            Assert.That(first, Is.True);
            Assert.That(second, Is.False);
            Assert.That(CombatSystem.ZombieThreatCount, Is.EqualTo(2));
        }

        [Test]
        public void SpawnedHordeUsesExistingKillFlow()
        {
            ZombieCardData zombie = CreateZombie("Target", health: 2, attack: 1);
            HordeProfileData profile = CreateProfile(
                HordeCompositionStyle.FixedOrder,
                baseBudget: 1,
                entries: new[] { new ArchetypeSetup(zombie, cost: 1, maxCount: 1) });
            ResolvedEventChoiceRuntime choice = ResolveChoice(
                new[] { CreateSpawnHordeEffect(profile) },
                CreateThreat(1, 0f));
            Assert.That(choice.TryExecute(GameEffectContext.ForEvent(), out _), Is.True);
            ZombieThreatRuntime spawned = CombatSystem.ZombieThreats[0];

            CombatSystem.AddPendingDamage(2);
            bool marked = CombatSystem.TryToggleZombieKillMark(spawned);
            int killed = CombatSystem.ResolveMarkedZombieKills();

            Assert.That(marked, Is.True);
            Assert.That(killed, Is.EqualTo(1));
            Assert.That(CombatSystem.ZombieThreatCount, Is.Zero);
        }

        [Test]
        public void SpawnHordeRespectsSafetyLimit()
        {
            ZombieCardData weak = CreateZombie("Weak", health: 1, attack: 1);
            HordeProfileData profile = CreateProfile(
                HordeCompositionStyle.FixedOrder,
                baseBudget: 300,
                entries: new[] { new ArchetypeSetup(weak, cost: 1) });
            ResolvedEventChoiceRuntime choice = ResolveChoice(
                new[] { CreateSpawnHordeEffect(profile) },
                CreateThreat(1, 0f));
            ResolvedHordeSpawnRuntime horde = choice.Effects[0].HordeSpawn;

            Assert.That(horde.CompositionResult.SpawnCount, Is.EqualTo(256));
            Assert.That(horde.StopReason, Is.EqualTo(HordeCompositionStopReason.SafetyLimitReached));
            Assert.That(choice.TryExecute(GameEffectContext.ForEvent(), out _), Is.True);
            Assert.That(CombatSystem.ZombieThreatCount, Is.EqualTo(256));
        }

        [Test]
        public void LegacyAddZombieThreatBehaviorRemainsAvailable()
        {
            GameEffectData legacy = new GameEffectData();
            SetField(legacy, "category", GameEffectCategory.Value);
            SetField(legacy, "valueEffectType", ValueEffectType.AddZombieThreat);
            SetField(legacy, "amount", 2);

            bool applied = legacy.TryApply(GameEffectContext.ForOther());

            Assert.That(applied, Is.True);
            Assert.That(CombatSystem.ZombieThreatCount, Is.EqualTo(2));
            Assert.That(CombatSystem.ZombieThreats[0].SourceCard, Is.Null);
            Assert.That(CombatSystem.ZombieThreats[1].SourceCard, Is.Null);
        }

        private ResolvedEventChoiceRuntime ResolveChoice(
            IReadOnlyList<GameEffectData> effects,
            ThreatSnapshot threat,
            int runSeed = 1,
            int eventDay = 1,
            string eventId = "EVENT-HORDE-TEST")
        {
            return ResolvedEventChoiceRuntime.Create(
                CreateChoiceData(effects),
                CreateMagnitude(eventDay),
                threat,
                runSeed,
                eventDay,
                eventId,
                choiceIndex: 0);
        }

        private EventData CreateEvent(string eventId, EventChoiceData choice)
        {
            EventData eventData = ScriptableObject.CreateInstance<EventData>();
            eventData.name = eventId;
            createdObjects.Add(eventData);
            SetField(eventData, "eventId", eventId);
            SetField(eventData, "displayTitle", eventId);
            SetField(eventData, "minimumDaysBetweenAppearances", 0);
            SetField(eventData, "choices", new List<EventChoiceData> { choice });
            return eventData;
        }

        private static EventChoiceData CreateChoiceData(IReadOnlyList<GameEffectData> effects)
        {
            EventChoiceData choice = new EventChoiceData();
            SetField(choice, "choiceText", "Face the horde");
            SetField(choice, "resultTitle", "The horde arrives");
            SetField(choice, "effects", new List<GameEffectData>(effects));
            return choice;
        }

        private static GameEffectData CreateSpawnHordeEffect(
            HordeProfileData profile,
            int eventBonusBudget = 0)
        {
            GameEffectData effect = new GameEffectData();
            SetField(effect, "carrier", GameEffectCarrier.Event);
            SetField(effect, "category", GameEffectCategory.Threat);
            SetField(effect, "threatEffectType", ThreatEffectType.SpawnHorde);
            SetField(effect, "hordeProfile", profile);
            SetField(effect, "eventBonusBudget", eventBonusBudget);
            SetField(effect, "amount", 999);
            return effect;
        }

        private static GameEffectData CreateResourceCost(int amount)
        {
            GameEffectData effect = new GameEffectData();
            SetField(effect, "carrier", GameEffectCarrier.Event);
            SetField(effect, "category", GameEffectCategory.Value);
            SetField(effect, "valueEffectType", ValueEffectType.SpendResource);
            SetField(effect, "amount", amount);
            SetField(effect, "eventPaymentMode", EventPaymentMode.ChoiceCost);
            return effect;
        }

        private static GameEffectData CreateClearFlagEffect(string flagId)
        {
            GameEffectData effect = new GameEffectData();
            SetField(effect, "carrier", GameEffectCarrier.Event);
            SetField(effect, "category", GameEffectCategory.EventState);
            SetField(effect, "eventStateEffectType", EventStateEffectType.ClearRunFlag);
            SetField(effect, "runFlagId", flagId);
            return effect;
        }

        private ZombieCardData CreateZombie(string displayName, int health, int attack)
        {
            ZombieCardData zombie = ScriptableObject.CreateInstance<ZombieCardData>();
            zombie.name = displayName;
            createdObjects.Add(zombie);
            SerializedObject serialized = new SerializedObject(zombie);
            SerializedProperty displayNameProperty = serialized.FindProperty("displayName");
            if (displayNameProperty != null)
            {
                displayNameProperty.stringValue = displayName;
            }

            serialized.FindProperty("health").intValue = health;
            serialized.FindProperty("attack").intValue = attack;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return zombie;
        }

        private HordeProfileData CreateProfile(
            HordeCompositionStyle style,
            int baseBudget,
            int worldThreatBudgetPerTier = 0,
            float baseAttentionBudgetPerPoint = 0f,
            params ArchetypeSetup[] entries)
        {
            HordeProfileData profile = ScriptableObject.CreateInstance<HordeProfileData>();
            profile.name = "TEST-HORDE";
            createdObjects.Add(profile);
            SerializedObject serialized = new SerializedObject(profile);
            serialized.FindProperty("profileId").stringValue = Guid.NewGuid().ToString("N");
            serialized.FindProperty("compositionStyle").enumValueIndex = (int)style;
            serialized.FindProperty("baseBudget").intValue = baseBudget;
            serialized.FindProperty("worldThreatBudgetPerTier").intValue = worldThreatBudgetPerTier;
            serialized.FindProperty("baseAttentionBudgetPerPoint").floatValue = baseAttentionBudgetPerPoint;
            SerializedProperty pool = serialized.FindProperty("zombiePool");
            pool.arraySize = entries != null ? entries.Length : 0;
            for (int index = 0; index < pool.arraySize; index++)
            {
                ArchetypeSetup setup = entries[index];
                SerializedProperty element = pool.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("zombieCard").objectReferenceValue = setup.Zombie;
                element.FindPropertyRelative("budgetCost").intValue = setup.Cost;
                element.FindPropertyRelative("baseWeight").intValue = setup.Weight;
                element.FindPropertyRelative("minWorldThreatTier").intValue = setup.MinTier;
                element.FindPropertyRelative("maxWorldThreatTier").intValue = 0;
                element.FindPropertyRelative("maxCountPerHorde").intValue = setup.MaxCount;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        private static EventMagnitudeSnapshot CreateMagnitude(int day)
        {
            EventMagnitudeRuntime runtime = new EventMagnitudeRuntime();
            runtime.InitializeRun();
            return runtime.Capture(new EventMagnitudeInputSnapshot(
                day,
                survivorCount: 1,
                additionalSurvivorCount: 0,
                activeBuildingLevelSum: 0,
                permanentSlotUpgradeCount: 0,
                otherPermanentDevelopmentModifiers: 0,
                maxEnergy: 1,
                foodStorageCapacity: 50,
                foodStorageCapacityBonus: 0));
        }

        private static ThreatSnapshot CreateThreat(int worldThreatTier, float baseAttention)
        {
            return new ThreatSnapshot(
                new ThreatInputSnapshot(day: 1),
                timeThreat: 0,
                developmentThreat: 0,
                worldThreat: Math.Max(0, worldThreatTier - 1),
                worldThreatTier: worldThreatTier,
                worldThreatBand: ThreatBand.Low,
                baseAttention: baseAttention,
                baseAttentionBand: ThreatBand.Low);
        }

        private static void SetField<T>(object target, string fieldName, T value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing private field '{fieldName}'.");
            field.SetValue(target, value);
        }

        private readonly struct ArchetypeSetup
        {
            public ArchetypeSetup(
                ZombieCardData zombie,
                int cost,
                int weight = 10,
                int minTier = 1,
                int maxCount = 0)
            {
                Zombie = zombie;
                Cost = cost;
                Weight = weight;
                MinTier = minTier;
                MaxCount = maxCount;
            }

            public ZombieCardData Zombie { get; }
            public int Cost { get; }
            public int Weight { get; }
            public int MinTier { get; }
            public int MaxCount { get; }
        }
    }
}
