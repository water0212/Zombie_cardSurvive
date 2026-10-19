using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ZombieCardSurvive.Cards.Data;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Effects.Data;
using ZombieCardSurvive.Events.Data;
using ZombieCardSurvive.Run;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Tests.Editor
{
    public sealed class NextRoundEffectTests
    {
        [SetUp]
        public void SetUp()
        {
            FoodSystem.Reset(0);
            FoodPlanSystem.Reset(baseConsumption: 0);
            EnergySystem.Reset(0);
            CombatSystem.Reset();
            MoraleSystem.Reset(highDecayPerRound: 0);
            NextRoundEffectSystem.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            FoodSystem.Reset(0);
            FoodPlanSystem.Reset(baseConsumption: 0);
            EnergySystem.Reset(0);
            CombatSystem.Reset();
            MoraleSystem.Reset(highDecayPerRound: 0);
            NextRoundEffectSystem.Reset();
        }

        [Test]
        public void SpawnTiming_AssignsFirstEligibleRoundEndFromCurrentPhase()
        {
            ZombieCardData zombie = CreateZombie();
            try
            {
                CombatSystem.BeginRound(3);
                ZombieThreatRuntime turnSpawn = CombatSystem.SpawnZombieThreat(zombie);

                CombatSystem.BeginRoundEnd(3);
                ZombieThreatRuntime roundEndSpawn = CombatSystem.SpawnZombieThreat(zombie);

                Assert.That(turnSpawn.FirstConsequenceRoundEnd, Is.EqualTo(3));
                Assert.That(turnSpawn.CanResolveConsequencesAtRoundEnd(3), Is.True);
                Assert.That(roundEndSpawn.FirstConsequenceRoundEnd, Is.EqualTo(4));
                Assert.That(roundEndSpawn.CanResolveConsequencesAtRoundEnd(3), Is.False);
                Assert.That(roundEndSpawn.CanResolveConsequencesAtRoundEnd(4), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(zombie);
            }
        }

        [Test]
        public void RoundEndSpawn_WaitsUntilFollowingRoundEndBeforeDamagingMorale()
        {
            ZombieCardData zombie = CreateZombie(attack: 5);
            RoundEndResolver resolver = CreateResolver();
            try
            {
                CombatSystem.BeginRoundEnd(1);
                CombatSystem.SpawnZombieThreat(zombie);

                RoundEndReport first = resolver.ResolveRoundEnd(null, 1, 3);
                Assert.That(first.EligibleZombieEffects, Is.Zero);
                Assert.That(first.DeferredZombieEffects, Is.EqualTo(1));
                Assert.That(MoraleSystem.CurrentMorale, Is.EqualTo(100));

                CombatSystem.BeginRound(2);
                RoundEndReport second = resolver.ResolveRoundEnd(null, 2, 6);
                Assert.That(second.EligibleZombieEffects, Is.EqualTo(1));
                Assert.That(second.ZombieMoraleDamage, Is.EqualTo(5));
                Assert.That(MoraleSystem.CurrentMorale, Is.EqualTo(95));
            }
            finally
            {
                Object.DestroyImmediate(zombie);
                Object.DestroyImmediate(resolver.gameObject);
            }
        }

        [Test]
        public void MarkedZombieKilledBeforeConsequences_CausesNoLossOrSchedule()
        {
            NextRoundEffectData consequence = CreateFoodDemandConsequence(3);
            ZombieCardData zombie = CreateZombie(attack: 5, permanentDamage: 4, consequence: consequence);
            RoundEndResolver resolver = CreateResolver();
            try
            {
                CombatSystem.BeginRound(1);
                ZombieThreatRuntime threat = CombatSystem.SpawnZombieThreat(zombie);
                CombatSystem.AddPendingDamage(threat.CurrentHealth);
                Assert.That(CombatSystem.TryToggleZombieKillMark(threat), Is.True);

                RoundEndReport report = resolver.ResolveRoundEnd(null, 1, 3);

                Assert.That(report.ZombiesKilled, Is.EqualTo(1));
                Assert.That(report.EligibleZombieEffects, Is.Zero);
                Assert.That(report.ZombieMoraleDamage, Is.Zero);
                Assert.That(report.ZombiePermanentMaximumMoraleDamage, Is.Zero);
                Assert.That(report.ZombieNextRoundEffectsScheduled, Is.Zero);
                Assert.That(MoraleSystem.CurrentMorale, Is.EqualTo(100));
                Assert.That(MoraleSystem.MaximumMorale, Is.EqualTo(100));
                Assert.That(NextRoundEffectSystem.Pending, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(zombie);
                Object.DestroyImmediate(consequence);
                Object.DestroyImmediate(resolver.gameObject);
            }
        }

        [Test]
        public void PermanentDamage_ClampsCurrentThenAppliesAttackWithoutDuplicateLoss()
        {
            ZombieCardData zombie = CreateZombie(attack: 4, permanentDamage: 7);
            RoundEndResolver resolver = CreateResolver();
            try
            {
                CombatSystem.BeginRound(1);
                CombatSystem.SpawnZombieThreat(zombie);

                RoundEndReport report = resolver.ResolveRoundEnd(null, 1, 3);

                Assert.That(report.ZombieMoraleDamage, Is.EqualTo(4));
                Assert.That(report.ZombiePermanentMaximumMoraleDamage, Is.EqualTo(7));
                Assert.That(report.MoraleSettlement.NetMoraleDelta, Is.EqualTo(-4));
                Assert.That(report.MoraleSettlement.NetMaximumMoraleDelta, Is.EqualTo(-7));
                Assert.That(MoraleSystem.MaximumMorale, Is.EqualTo(93));
                Assert.That(MoraleSystem.CurrentMorale, Is.EqualTo(89));
            }
            finally
            {
                Object.DestroyImmediate(zombie);
                Object.DestroyImmediate(resolver.gameObject);
            }
        }

        [Test]
        public void PermanentDamagePreview_DoesNotChargeMealOrAddHighMoraleDriftAfterClamp()
        {
            ZombieCardData zombie = CreateZombie(permanentDamage: 50);
            RoundEndResolver resolver = CreateResolver(
                FoodPlanLevel.Generous,
                baseFoodDemand: 0,
                highMoraleDecay: 3);
            FoodSystem.Reset(10);
            FoodPlanSystem.Reset(FoodPlanLevel.Generous, baseConsumption: 0);
            EnergySystem.Reset(3);
            MoraleSystem.Reset(80, highDecayPerRound: 3);
            try
            {
                CombatSystem.BeginRound(1);
                CombatSystem.SpawnZombieThreat(zombie);

                RoundEndReport report = resolver.ResolveRoundEnd(null, 1, 3);

                Assert.That(report.EncouragingMealAttempted, Is.True);
                Assert.That(report.EncouragingMealSucceeded, Is.False);
                Assert.That(report.EncouragingMealExtraFoodPaid, Is.Zero);
                Assert.That(report.HighMoraleDrift, Is.Zero);
                Assert.That(FoodSystem.GetFood(), Is.EqualTo(10));
                Assert.That(MoraleSystem.MaximumMorale, Is.EqualTo(50));
                Assert.That(MoraleSystem.CurrentMorale, Is.EqualTo(50));
            }
            finally
            {
                Object.DestroyImmediate(zombie);
                Object.DestroyImmediate(resolver.gameObject);
            }
        }

        [Test]
        public void ZombieSchedule_LocksAtRoundEndAndSurvivesSourceRemoval()
        {
            NextRoundEffectData consequence = CreateFoodDemandConsequence(3);
            ZombieCardData zombie = CreateZombie(consequence: consequence);
            RoundEndResolver resolver = CreateResolver();
            try
            {
                CombatSystem.BeginRound(1);
                CombatSystem.SpawnZombieThreat(zombie);

                RoundEndReport first = resolver.ResolveRoundEnd(null, 1, 3);
                Assert.That(first.ZombieNextRoundEffectsScheduled, Is.EqualTo(1));
                Assert.That(first.NextRoundFoodDemandScheduled, Is.EqualTo(3));
                Assert.That(FoodPlanSystem.TemporaryRoundConsumptionModifier, Is.Zero);

                Assert.That(CombatSystem.RemoveZombieThreat(1), Is.True);
                NextRoundEffectSystem.BeginRound(2);

                Assert.That(FoodPlanSystem.TemporaryRoundConsumptionModifier, Is.EqualTo(3));
                Assert.That(NextRoundEffectSystem.Pending, Is.Empty);
                Assert.That(NextRoundEffectSystem.LastActivated.Count, Is.EqualTo(1));
                Assert.That(
                    NextRoundEffectSystem.LastActivated[0].SourceKind,
                    Is.EqualTo(NextRoundEffectSourceKind.Zombie));
            }
            finally
            {
                Object.DestroyImmediate(zombie);
                Object.DestroyImmediate(consequence);
                Object.DestroyImmediate(resolver.gameObject);
            }
        }

        [Test]
        public void SurvivingZombie_SchedulesASeparateOneShotForEachEligibleRound()
        {
            NextRoundEffectData consequence = CreateFoodDemandConsequence(3);
            ZombieCardData zombie = CreateZombie(consequence: consequence);
            RoundEndResolver resolver = CreateResolver();
            try
            {
                CombatSystem.BeginRound(1);
                CombatSystem.SpawnZombieThreat(zombie);
                resolver.ResolveRoundEnd(null, 1, 3);

                NextRoundEffectSystem.BeginRound(2);
                Assert.That(NextRoundEffectSystem.Pending, Is.Empty);
                Assert.That(FoodPlanSystem.TemporaryRoundConsumptionModifier, Is.EqualTo(3));

                RoundEndReport second = resolver.ResolveRoundEnd(null, 2, 6);
                Assert.That(second.FoodConsumption, Is.EqualTo(3));
                Assert.That(second.ZombieNextRoundEffectsScheduled, Is.EqualTo(1));
                Assert.That(NextRoundEffectSystem.Pending.Count, Is.EqualTo(1));
                Assert.That(NextRoundEffectSystem.Pending[0].TargetRound, Is.EqualTo(3));
            }
            finally
            {
                Object.DestroyImmediate(zombie);
                Object.DestroyImmediate(consequence);
                Object.DestroyImmediate(resolver.gameObject);
            }
        }

        [Test]
        public void CardConsequence_SchedulesOnlyForFollowingRound()
        {
            NextRoundEffectData consequence = CreateFoodDemandConsequence(3);
            CardBase card = CreateConsequenceCard(consequence, GameEffectCarrier.Card);
            try
            {
                NextRoundEffectSystem.BeginRound(4);
                card.Resolve(new CardPlayContext(null, null));

                Assert.That(FoodPlanSystem.TemporaryRoundConsumptionModifier, Is.Zero);
                Assert.That(NextRoundEffectSystem.Pending.Count, Is.EqualTo(1));
                Assert.That(NextRoundEffectSystem.Pending[0].TargetRound, Is.EqualTo(5));

                NextRoundEffectSystem.BeginRound(5);
                Assert.That(FoodPlanSystem.TemporaryRoundConsumptionModifier, Is.EqualTo(3));
            }
            finally
            {
                Object.DestroyImmediate(card);
                Object.DestroyImmediate(consequence);
            }
        }

        [Test]
        public void EventConsequence_SchedulesOnSuccessButNotWhenPaymentValidationFails()
        {
            NextRoundEffectData consequence = CreateFoodDemandConsequence(3);
            EventChoiceData success = CreateEventChoice(consequence, includeUnpayableCost: false);
            EventChoiceData blocked = CreateEventChoice(consequence, includeUnpayableCost: true);
            try
            {
                NextRoundEffectSystem.BeginRound(2);
                success.ApplyEffects(GameEffectContext.ForEvent(null, 3));
                Assert.That(NextRoundEffectSystem.Pending.Count, Is.EqualTo(1));
                Assert.That(
                    NextRoundEffectSystem.Pending[0].SourceKind,
                    Is.EqualTo(NextRoundEffectSourceKind.Event));

                NextRoundEffectSystem.Reset(2);
                blocked.ApplyEffects(GameEffectContext.ForEvent(null, 3));
                Assert.That(NextRoundEffectSystem.Pending, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(consequence);
            }
        }

        [Test]
        public void MixedSources_StackAndResetClearsAllPendingEntries()
        {
            NextRoundEffectData consequence = CreateFoodDemandConsequence(3);
            try
            {
                NextRoundEffectSystem.BeginRound(1);
                Assert.That(NextRoundEffectSystem.TryScheduleForNextRound(
                    consequence,
                    NextRoundEffectSourceKind.Zombie,
                    "1",
                    out _), Is.True);
                Assert.That(NextRoundEffectSystem.TryScheduleForNextRound(
                    consequence,
                    NextRoundEffectSourceKind.Card,
                    "card",
                    out _), Is.True);
                Assert.That(NextRoundEffectSystem.TryScheduleForNextRound(
                    consequence,
                    NextRoundEffectSourceKind.Event,
                    "event",
                    out _), Is.True);

                NextRoundEffectSystem.BeginRound(2);
                Assert.That(FoodPlanSystem.TemporaryRoundConsumptionModifier, Is.EqualTo(9));
                Assert.That(NextRoundEffectSystem.LastActivated.Count, Is.EqualTo(3));

                NextRoundEffectSystem.TryScheduleForNextRound(
                    consequence,
                    NextRoundEffectSourceKind.Card,
                    "card-2",
                    out _);
                NextRoundEffectSystem.Reset();
                Assert.That(NextRoundEffectSystem.Pending, Is.Empty);
                Assert.That(NextRoundEffectSystem.LastActivated, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(consequence);
            }
        }

        private static RoundEndResolver CreateResolver(
            FoodPlanLevel startingPlan = FoodPlanLevel.Normal,
            int baseFoodDemand = 0,
            int highMoraleDecay = 0)
        {
            GameObject owner = new GameObject("NextRoundEffectResolverTest");
            RoundEndResolver resolver = owner.AddComponent<RoundEndResolver>();
            SerializedObject serialized = new SerializedObject(resolver);
            serialized.FindProperty("startingFoodPlan").enumValueIndex = (int)startingPlan;
            serialized.FindProperty("baseRoundFoodConsumption").intValue = baseFoodDemand;
            serialized.FindProperty("highMoraleDecayPerRound").intValue = highMoraleDecay;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return resolver;
        }

        private static ZombieCardData CreateZombie(
            int attack = 0,
            int permanentDamage = 0,
            NextRoundEffectData consequence = null)
        {
            ZombieCardData zombie = ScriptableObject.CreateInstance<ZombieCardData>();
            SerializedObject serialized = new SerializedObject(zombie);
            serialized.FindProperty("health").intValue = 1;
            serialized.FindProperty("attack").intValue = attack;
            serialized.FindProperty("permanentMaximumMoraleDamage").intValue = permanentDamage;
            serialized.FindProperty("survivalNextRoundEffect").objectReferenceValue = consequence;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return zombie;
        }

        private static NextRoundEffectData CreateFoodDemandConsequence(int amount)
        {
            NextRoundEffectData consequence =
                ScriptableObject.CreateInstance<NextRoundEffectData>();
            SerializedObject serialized = new SerializedObject(consequence);
            SerializedProperty effects = serialized.FindProperty("effects");
            effects.arraySize = 1;
            SerializedProperty entry = effects.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("effectType").enumValueIndex =
                (int)NextRoundEffectType.IncreaseNextRoundFoodDemand;
            entry.FindPropertyRelative("amount").intValue = amount;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return consequence;
        }

        private static CardBase CreateConsequenceCard(
            NextRoundEffectData consequence,
            GameEffectCarrier carrier)
        {
            CardBase card = ScriptableObject.CreateInstance<CardBase>();
            SerializedObject serialized = new SerializedObject(card);
            SerializedProperty effects = serialized.FindProperty("effects");
            effects.arraySize = 1;
            ConfigureConsequenceEffect(effects.GetArrayElementAtIndex(0), consequence, carrier);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return card;
        }

        private static EventChoiceData CreateEventChoice(
            NextRoundEffectData consequence,
            bool includeUnpayableCost)
        {
            EventChoiceData choice = new EventChoiceData();
            GameEffectData consequenceEffect = new GameEffectData();
            SetField(consequenceEffect, "carrier", GameEffectCarrier.Event);
            SetField(consequenceEffect, "category", GameEffectCategory.NextRoundEffect);
            SetField(consequenceEffect, "nextRoundEffect", consequence);

            System.Collections.Generic.List<GameEffectData> effects =
                new System.Collections.Generic.List<GameEffectData>();
            if (includeUnpayableCost)
            {
                GameEffectData cost = new GameEffectData();
                SetField(cost, "carrier", GameEffectCarrier.Event);
                SetField(cost, "category", GameEffectCategory.Value);
                SetField(cost, "valueEffectType", ValueEffectType.SpendFood);
                SetField(cost, "amount", 1);
                effects.Add(cost);
            }

            effects.Add(consequenceEffect);
            SetField(choice, "effects", effects);
            return choice;
        }

        private static void ConfigureConsequenceEffect(
            SerializedProperty effect,
            NextRoundEffectData consequence,
            GameEffectCarrier carrier)
        {
            effect.FindPropertyRelative("carrier").enumValueIndex = (int)carrier;
            effect.FindPropertyRelative("category").enumValueIndex =
                (int)GameEffectCategory.NextRoundEffect;
            effect.FindPropertyRelative("nextRoundEffect").objectReferenceValue = consequence;
        }

        private static void SetField(object target, string name, object value)
        {
            System.Reflection.FieldInfo field = target.GetType().GetField(
                name,
                System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {name} on {target.GetType().Name}.");
            field.SetValue(target, value);
        }
    }
}
