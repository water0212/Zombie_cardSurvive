using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using ZombieCardSurvive.Buildings.Data;
using ZombieCardSurvive.Buildings.Runtime;
using ZombieCardSurvive.Cards.Data;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Tests.Editor
{
    public class BuildingRuntimeTests
    {
        [SetUp]
        public void SetUp()
        {
            ResourceReservationService.Reset();
            FoodSystem.Reset(10);
            ResourceSystem.Reset(5);
        }

        [TearDown]
        public void TearDown()
        {
            ResourceReservationService.Reset();
        }

        [Test]
        public void ReservedResources_AreProtectedUntilCommitted()
        {
            Assert.That(ResourceReservationService.TryReserve(new ResourceCost(4, 3), out ResourceReservationToken token), Is.True);
            Assert.That(FoodSystem.AvailableFood, Is.EqualTo(6));
            Assert.That(ResourceSystem.AvailableResource, Is.EqualTo(2));
            Assert.That(FoodSystem.SpendFood(7), Is.False);

            int shortage = FoodSystem.ConsumeFoodAllowShortage(8);

            Assert.That(shortage, Is.EqualTo(2));
            Assert.That(FoodSystem.GetFood(), Is.EqualTo(4));
            Assert.That(ResourceReservationService.Commit(token), Is.True);
            Assert.That(FoodSystem.GetFood(), Is.Zero);
            Assert.That(ResourceSystem.GetResource(), Is.EqualTo(2));
        }

        [Test]
        public void Reservation_ReleaseAndResetRestoreAvailability()
        {
            Assert.That(ResourceReservationService.TryReserve(new ResourceCost(3, 2), out ResourceReservationToken token), Is.True);
            Assert.That(FoodSystem.AvailableFood, Is.EqualTo(7));
            Assert.That(ResourceSystem.AvailableResource, Is.EqualTo(3));

            Assert.That(ResourceReservationService.Release(token), Is.True);
            Assert.That(FoodSystem.AvailableFood, Is.EqualTo(10));
            Assert.That(ResourceSystem.AvailableResource, Is.EqualTo(5));

            ResourceReservationService.TryReserve(new ResourceCost(2, 1), out _);
            ResourceReservationService.Reset();
            Assert.That(ResourceReservationService.ReservedFood, Is.Zero);
            Assert.That(ResourceReservationService.ReservedResource, Is.Zero);
        }

        [Test]
        public void CraftJob_ReturnsOverflowWhenProductionExceedsRequirement()
        {
            CardCraftRecipeData recipe = ScriptableObject.CreateInstance<CardCraftRecipeData>();
            SerializedObject serialized = new SerializedObject(recipe);
            serialized.FindProperty("requiredProduction").intValue = 4;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            ResourceReservationService.TryReserve(default, out ResourceReservationToken token);
            CraftJobRuntime job = new CraftJobRuntime(recipe, token);

            int overflow = job.ApplyProduction(5);

            Assert.That(job.CurrentProduction, Is.EqualTo(4));
            Assert.That(overflow, Is.EqualTo(1));
            Object.DestroyImmediate(recipe);
        }

        [Test]
        public void IndependentCraftLines_KeepProgressAndReceiveFullProduction()
        {
            CardCraftRecipeData recipe = CreateRecipe(5);
            CardCraftInteractionData firstData = CreateCraftLineData(2);
            CardCraftInteractionData secondData = CreateCraftLineData(2);
            ResourceReservationService.TryReserve(default, out ResourceReservationToken firstToken);
            ResourceReservationService.TryReserve(default, out ResourceReservationToken secondToken);
            CraftLineRuntime firstLine = new CraftLineRuntime(firstData);
            CraftLineRuntime secondLine = new CraftLineRuntime(secondData);
            firstLine.AddJob(new CraftJobRuntime(recipe, firstToken));
            secondLine.AddJob(new CraftJobRuntime(recipe, secondToken));

            firstLine.Jobs[0].ApplyProduction(firstLine.Data.BaseProduction);
            secondLine.Jobs[0].ApplyProduction(secondLine.Data.BaseProduction);

            Assert.That(firstLine.Jobs[0].CurrentProduction, Is.EqualTo(2));
            Assert.That(secondLine.Jobs[0].CurrentProduction, Is.EqualTo(2));
            Assert.That(firstLine.Jobs[0].RemainingProduction, Is.EqualTo(3));
            firstLine.Jobs[0].ApplyProduction(firstLine.Data.BaseProduction);
            Assert.That(firstLine.Jobs[0].CurrentProduction, Is.EqualTo(4));
            Assert.That(secondLine.Jobs[0].CurrentProduction, Is.EqualTo(2));

            Object.DestroyImmediate(recipe);
            Object.DestroyImmediate(firstData);
            Object.DestroyImmediate(secondData);
        }

        [Test]
        public void ConstructionJob_TracksUniqueCardsAndClampsProgress()
        {
            BuildingLevelData level = new BuildingLevelData();
            ConstructionJobRuntime job = new ConstructionJobRuntime(level, default);

            job.AddCardInstance("construction-card-1");
            job.AddCardInstance("construction-card-1");
            job.AddProgress();
            job.AddProgress();

            Assert.That(job.ConstructionCardInstanceIds.Count, Is.EqualTo(1));
            Assert.That(job.CurrentProgress, Is.EqualTo(level.RequiredConstructionProgress));
            Assert.That(job.IsProgressComplete, Is.True);
        }

        [Test]
        public void RemovingRegularConstructionCard_CreatesVacancy()
        {
            GameObject controllerObject = new GameObject("CardControllerTest");
            CardController controller = controllerObject.AddComponent<CardController>();
            ConstructionCardData card = ScriptableObject.CreateInstance<ConstructionCardData>();
            Assert.That(controller.AddCardToDiscardPile(card), Is.True);
            string runtimeId = controller.DiscardPile[0].RuntimeId;

            int removed = controller.RemoveRuntimeCardsByInstanceIds(
                new[] { runtimeId },
                DeckVacancyReason.BuildingCancelled);

            Assert.That(removed, Is.EqualTo(1));
            Assert.That(controller.VacancyRecords.Count, Is.EqualTo(1));
            Assert.That(controller.VacancyRecords[0].Reason, Is.EqualTo(DeckVacancyReason.BuildingCancelled));
            Object.DestroyImmediate(card);
            Object.DestroyImmediate(controllerObject);
        }

        [Test]
        public void FillingBuildingVacancy_DoesNotReturnRemovedConstructionCardToInventory()
        {
            GameObject controllerObject = new GameObject("VacancyControllerTest");
            GameObject inventoryObject = new GameObject("VacancyInventoryTest");
            CardController controller = controllerObject.AddComponent<CardController>();
            CardInventory inventory = inventoryObject.AddComponent<CardInventory>();
            ConstructionCardData constructionCard = ScriptableObject.CreateInstance<ConstructionCardData>();
            FoodCardData replacementCard = ScriptableObject.CreateInstance<FoodCardData>();
            controller.AddCardToDiscardPile(constructionCard);
            CardRuntime removedTarget = controller.DiscardPile[0];
            controller.RemoveRuntimeCardsByInstanceIds(
                new[] { removedTarget.RuntimeId },
                DeckVacancyReason.BuildingCompleted);
            CardInventoryEntry replacement = inventory.AddCard(replacementCard);
            DeckReplacementRequest request = new DeckReplacementRequest(
                DeckReplacementReason.VacancyRefill,
                1,
                new[] { removedTarget },
                new[] { replacement });

            bool result = DeckReplacementSystem.TryReplace(
                controller,
                inventory,
                null,
                request,
                removedTarget,
                replacement,
                out _);

            Assert.That(result, Is.True);
            Assert.That(controller.VacancyRecords.Count, Is.Zero);
            Assert.That(inventory.FindEntry(removedTarget.RuntimeId), Is.Null);
            Assert.That(inventory.Count, Is.Zero);
            Object.DestroyImmediate(replacementCard);
            Object.DestroyImmediate(constructionCard);
            Object.DestroyImmediate(inventoryObject);
            Object.DestroyImmediate(controllerObject);
        }

        [Test]
        public void NormalReplacement_ReturnsReplacedCardToInventory()
        {
            GameObject controllerObject = new GameObject("ReplacementControllerTest");
            GameObject inventoryObject = new GameObject("ReplacementInventoryTest");
            CardController controller = controllerObject.AddComponent<CardController>();
            CardInventory inventory = inventoryObject.AddComponent<CardInventory>();
            FoodCardData originalCard = ScriptableObject.CreateInstance<FoodCardData>();
            ResourceCardData replacementCard = ScriptableObject.CreateInstance<ResourceCardData>();
            controller.AddCardToDiscardPile(originalCard);
            CardRuntime target = controller.DiscardPile[0];
            CardInventoryEntry replacement = inventory.AddCard(replacementCard);
            DeckReplacementRequest request = new DeckReplacementRequest(
                DeckReplacementReason.Planning,
                1,
                new[] { target },
                new[] { replacement });

            bool result = DeckReplacementSystem.TryReplace(
                controller,
                inventory,
                null,
                request,
                target,
                replacement,
                out _);

            Assert.That(result, Is.True);
            Assert.That(inventory.FindEntry(target.RuntimeId), Is.Not.Null);
            Assert.That(inventory.Count, Is.EqualTo(1));
            Object.DestroyImmediate(replacementCard);
            Object.DestroyImmediate(originalCard);
            Object.DestroyImmediate(inventoryObject);
            Object.DestroyImmediate(controllerObject);
        }

        private static CardCraftRecipeData CreateRecipe(int requiredProduction)
        {
            CardCraftRecipeData recipe = ScriptableObject.CreateInstance<CardCraftRecipeData>();
            SerializedObject serialized = new SerializedObject(recipe);
            serialized.FindProperty("requiredProduction").intValue = requiredProduction;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return recipe;
        }

        private static CardCraftInteractionData CreateCraftLineData(int production)
        {
            CardCraftInteractionData data = ScriptableObject.CreateInstance<CardCraftInteractionData>();
            SerializedObject serialized = new SerializedObject(data);
            serialized.FindProperty("baseProduction").intValue = production;
            serialized.FindProperty("queueCapacity").intValue = 3;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }
    }
}
