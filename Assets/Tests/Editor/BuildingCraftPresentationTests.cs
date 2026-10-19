using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ZombieCardSurvive.Buildings.Data;
using ZombieCardSurvive.Buildings.Runtime;
using ZombieCardSurvive.Buildings.UI;

namespace ZombieCardSurvive.Tests.Editor
{
    public sealed class BuildingCraftPresentationTests
    {
        private GameObject root;
        private CardCraftView view;
        private BuildBaseData buildingData;
        private CardCraftInteractionData interaction;
        private CardCraftRecipeData first, second;
        private BuildingRuntime building;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Craft presentation test");
            view = root.AddComponent<CardCraftView>();
            buildingData = ScriptableObject.CreateInstance<BuildBaseData>();
            interaction = ScriptableObject.CreateInstance<CardCraftInteractionData>();
            first = ScriptableObject.CreateInstance<CardCraftRecipeData>();
            second = ScriptableObject.CreateInstance<CardCraftRecipeData>();
            ((List<CardCraftRecipeData>)interaction.Recipes).Add(first);
            ((List<CardCraftRecipeData>)interaction.Recipes).Add(second);
            building = new BuildingRuntime(buildingData);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(buildingData); Object.DestroyImmediate(interaction);
            Object.DestroyImmediate(first); Object.DestroyImmediate(second);
        }

        [Test]
        public void StaticLockedPreview_DoesNotCreateRuntimeOrReactivateHiddenPage()
        {
            root.SetActive(false);
            view.Bind(null, building, interaction);
            view.Refresh();
            Assert.That(root.activeSelf, Is.False);
            Assert.That(building.CraftLines, Is.Empty);
            Assert.That(view.GetUnavailableReason(first), Does.Contain("需要建築等級"));
        }

        [Test]
        public void ExpandRecipe_TogglesReadingSelection_WithoutCreatingJob()
        {
            view.Bind(null, building, interaction);
            view.ExpandRecipe(first);
            Assert.That(view.ExpandedRecipe, Is.SameAs(first));
            view.ExpandRecipe(second);
            view.Refresh();
            Assert.That(view.ExpandedRecipe, Is.SameAs(second));
            view.ExpandRecipe(second);
            Assert.That(view.ExpandedRecipe, Is.Null);
            Assert.That(building.CraftLines, Is.Empty);
            view.Unbind();
            Assert.That(view.ExpandedRecipe, Is.Null);
        }

        [Test]
        public void DuplicateRecipeJobs_SelectById_RemovalDoesNotSelectSibling()
        {
            var line = new CraftLineRuntime(interaction);
            ((List<CraftLineRuntime>)building.CraftLines).Add(line);
            var one = new CraftJobRuntime(first, default(ResourceReservationToken));
            var two = new CraftJobRuntime(first, default(ResourceReservationToken));
            // Fixture bypasses capacity only to isolate presentation identity.
            ((List<CraftJobRuntime>)line.Jobs).Add(one);
            ((List<CraftJobRuntime>)line.Jobs).Add(two);
            view.Bind(null, building, interaction);
            view.SelectJob(two.JobId);
            Assert.That(view.SelectedJobId, Is.EqualTo(two.JobId));
            Assert.That(view.ExpandedRecipe, Is.SameAs(first));
            line.RemoveJob(two);
            view.Refresh();
            Assert.That(view.SelectedJobId, Is.Null);
            Assert.That(line.Jobs.Count, Is.EqualTo(1));
            Assert.That(line.Jobs[0], Is.SameAs(one));
        }

        [Test]
        public void PendingCancellation_HasExplicitTextNotOnlyColour()
        {
            Assert.That(CraftJobRowView.StateLabel(BuildingJobState.CancelPending), Does.Contain("待取消"));
            Assert.That(CraftJobRowView.StateLabel(BuildingJobState.CancelPending), Does.Contain("結算"));
        }
    }
}
