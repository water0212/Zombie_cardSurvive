using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZombieCardSurvive.Cards.UI;
using ZombieCardSurvive.Exploration.Data;
using ZombieCardSurvive.Exploration.Runtime;
using ZombieCardSurvive.Exploration.UI;

namespace ZombieCardSurvive.Tests.Editor
{
    public class RegionContentV1SetupTests
    {
        private const string RegionFolder = "Assets/ScriptObj/RegionScriptObj/ContentV1";
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";

        private static readonly RegionExpectation[] Regions =
        {
            new RegionExpectation("REGION-001", "廢棄超市", ExplorationTier.Basic, DangerLevel.Low, 2, 0.15f, 0.10f, 0.10f),
            new RegionExpectation("REGION-002", "住宅區", ExplorationTier.Basic, DangerLevel.Low, 2, 0.15f, 0.10f, 0.10f),
            new RegionExpectation("REGION-003", "消防局", ExplorationTier.Basic, DangerLevel.Medium, 3, 0.30f, 0.10f, 0.10f),
            new RegionExpectation("REGION-004", "五金行", ExplorationTier.Advanced, DangerLevel.High, 4, 0.45f, 0.15f, 0.10f),
            new RegionExpectation("REGION-005", "倉庫", ExplorationTier.Advanced, DangerLevel.High, 6, 0.45f, 0.15f, 0.15f)
        };

        private static readonly PackExpectation[] Packs =
        {
            new PackExpectation("REGION-001", "PACK-COMMON-FOOD-01", RegionSupplyRarity.Common, 4, 70),
            new PackExpectation("REGION-001", "PACK-UNCOMMON-FOOD-01", RegionSupplyRarity.Rare, 2, 25),
            new PackExpectation("REGION-001", "PACK-RARE-SUPPLY-01", RegionSupplyRarity.UltraRare, 1, 5),

            new PackExpectation("REGION-002", "PACK-COMMON-RESOURCE-01", RegionSupplyRarity.Common, 2, 35),
            new PackExpectation("REGION-002", "PACK-COMMON-FOOD-01", RegionSupplyRarity.Common, 2, 35),
            new PackExpectation("REGION-002", "PACK-UNCOMMON-RESOURCE-01", RegionSupplyRarity.Rare, 2, 25),
            new PackExpectation("REGION-002", "PACK-REGION-002-01", RegionSupplyRarity.UltraRare, 1, 5),

            new PackExpectation("REGION-003", "PACK-COMMON-RESOURCE-01", RegionSupplyRarity.Common, 4, 70),
            new PackExpectation("REGION-003", "PACK-UNCOMMON-RESOURCE-01", RegionSupplyRarity.Rare, 2, 25),
            new PackExpectation("REGION-003", "PACK-FireDepartment-01", RegionSupplyRarity.UltraRare, 1, 5),

            new PackExpectation("REGION-004", "PACK-COMMON-RESOURCE-01", RegionSupplyRarity.Common, 3, 60),
            new PackExpectation("REGION-004", "PACK-UNCOMMON-RESOURCE-01", RegionSupplyRarity.Rare, 3, 30),
            new PackExpectation("REGION-004", "PACK-RARE-RESOURCE-01", RegionSupplyRarity.UltraRare, 2, 10),

            new PackExpectation("REGION-005", "PACK-UNCOMMON-FOOD-01", RegionSupplyRarity.Common, 4, 35),
            new PackExpectation("REGION-005", "PACK-UNCOMMON-RESOURCE-01", RegionSupplyRarity.Common, 4, 35),
            new PackExpectation("REGION-005", "PACK-RARE-SUPPLY-01", RegionSupplyRarity.Rare, 3, 25),
            new PackExpectation("REGION-005", "PACK-STOREHOUSE-01", RegionSupplyRarity.UltraRare, 2, 5)
        };

        [Test]
        public void RegionsMatchDesignSheet()
        {
            foreach (RegionExpectation expected in Regions)
            {
                ExplorationRegionData region = LoadRegion(expected.RegionId);

                Assert.That(region.DisplayName, Is.EqualTo(expected.DisplayName), expected.RegionId);
                Assert.That(region.RegionTier, Is.EqualTo(expected.Tier), expected.RegionId);
                Assert.That(region.DangerLevel, Is.EqualTo(expected.Danger), expected.RegionId);
                Assert.That(region.InitialZombieCount, Is.EqualTo(expected.ZombieCount), expected.RegionId);
                Assert.That(region.BaseEncounterChance, Is.EqualTo(expected.EncounterChance).Within(0.0001f), expected.RegionId);
                Assert.That(region.HeatPerExploration, Is.EqualTo(expected.HeatPerExploration).Within(0.0001f), expected.RegionId);
                Assert.That(region.DailyHeatDecay, Is.EqualTo(expected.DailyHeatDecay).Within(0.0001f), expected.RegionId);
            }
        }

        [Test]
        public void RegionPackDropsMatchDesignSheet()
        {
            foreach (IGrouping<string, PackExpectation> group in Packs.GroupBy(pack => pack.RegionId))
            {
                ExplorationRegionData region = LoadRegion(group.Key);
                PackExpectation[] expectedPacks = group.ToArray();

                Assert.That(region.CardPackDrops.Count, Is.EqualTo(expectedPacks.Length), group.Key);
                for (int index = 0; index < expectedPacks.Length; index++)
                {
                    CardPackDropEntry actual = region.CardPackDrops[index];
                    PackExpectation expected = expectedPacks[index];

                    Assert.That(actual, Is.Not.Null, $"{group.Key} entry {index}");
                    Assert.That(actual.CardPack, Is.Not.Null, $"{group.Key} entry {index}");
                    Assert.That(actual.CardPack.PackId, Is.EqualTo(expected.PackId), $"{group.Key} entry {index}");
                    Assert.That(actual.SupplyRarity, Is.EqualTo(expected.Rarity), $"{group.Key} entry {index}");
                    Assert.That(actual.InitialCount, Is.EqualTo(expected.InitialCount), $"{group.Key} entry {index}");
                    Assert.That(actual.BaseWeight, Is.EqualTo(expected.BaseWeight), $"{group.Key} entry {index}");
                    Assert.That(actual.MinPackCount, Is.EqualTo(1), $"{group.Key} entry {index}");
                    Assert.That(actual.MaxPackCount, Is.EqualTo(1), $"{group.Key} entry {index}");
                }
            }
        }

        [Test]
        public void ReferencedCardPacksHaveNormalizedIdsAndUsablePools()
        {
            Dictionary<string, CardPackData> referencedPacks = Packs
                .SelectMany(expected => LoadRegion(expected.RegionId).CardPackDrops)
                .Select(drop => drop.CardPack)
                .Distinct()
                .ToDictionary(pack => pack.PackId, StringComparer.Ordinal);

            Assert.That(referencedPacks.Count, Is.EqualTo(9));
            foreach (string packId in Packs.Select(pack => pack.PackId).Distinct())
            {
                Assert.That(referencedPacks.ContainsKey(packId), Is.True, packId);
                CardPackData pack = referencedPacks[packId];
                Assert.That(pack.PackId, Is.EqualTo(pack.PackId.Trim()), packId);
                Assert.That(pack.CardsToGenerate, Is.GreaterThan(0), packId);
                Assert.That(pack.CardPool.Any(entry => entry != null && entry.Card != null && entry.Weight > 0), Is.True, packId);
            }
        }

        [Test]
        public void SampleSceneUsesCanonicalRegionPoolInSheetOrder()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool closeWhenDone = !scene.IsValid() || !scene.isLoaded;
            if (closeWhenDone)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            try
            {
                List<ExplorationSystem> systems = new List<ExplorationSystem>();
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    systems.AddRange(root.GetComponentsInChildren<ExplorationSystem>(true));
                }

                Assert.That(systems.Count, Is.EqualTo(1));
                SerializedProperty pool = new SerializedObject(systems[0]).FindProperty("regionPool");
                Assert.That(pool.arraySize, Is.EqualTo(Regions.Length));

                for (int index = 0; index < Regions.Length; index++)
                {
                    ExplorationRegionData region =
                        pool.GetArrayElementAtIndex(index).objectReferenceValue as ExplorationRegionData;
                    Assert.That(region, Is.Not.Null, $"Region Pool entry {index}");
                    Assert.That(region.RegionId, Is.EqualTo(Regions[index].RegionId), $"Region Pool entry {index}");
                }
            }
            finally
            {
                if (closeWhenDone && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        [Test]
        public void SampleSceneExplorationPresentationIsFullyWired()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool closeWhenDone = !scene.IsValid() || !scene.isLoaded;
            if (closeWhenDone)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            try
            {
                ExplorationSystem system = FindSceneComponents<ExplorationSystem>(scene).Single();
                ExplorationFlowView flowView = FindSceneComponents<ExplorationFlowView>(scene).Single();
                SerializedObject flowSerialized = new SerializedObject(flowView);

                Assert.That(
                    flowSerialized.FindProperty("explorationSystem").objectReferenceValue,
                    Is.SameAs(system));

                SerializedProperty regionCards = flowSerialized.FindProperty("regionCards");
                Assert.That(regionCards.arraySize, Is.GreaterThanOrEqualTo(3));
                for (int index = 0; index < regionCards.arraySize; index++)
                {
                    ExplorationRegionCardView card =
                        regionCards.GetArrayElementAtIndex(index).objectReferenceValue as ExplorationRegionCardView;
                    Assert.That(card, Is.Not.Null, $"Region card {index}");

                    SerializedObject cardSerialized = new SerializedObject(card);
                    Assert.That(cardSerialized.FindProperty("button").objectReferenceValue, Is.Not.Null, $"Region card {index} button");
                    Assert.That(cardSerialized.FindProperty("nameText").objectReferenceValue, Is.Not.Null, $"Region card {index} name");
                    Assert.That(cardSerialized.FindProperty("descriptionText").objectReferenceValue, Is.Not.Null, $"Region card {index} description");
                    Assert.That(cardSerialized.FindProperty("dangerText").objectReferenceValue, Is.Not.Null, $"Region card {index} status");
                    Assert.That(cardSerialized.FindProperty("rewardsText").objectReferenceValue, Is.Not.Null, $"Region card {index} rewards");
                }

                ResourceDebugView debugView = FindSceneComponents<ResourceDebugView>(scene).Single();
                SerializedObject debugSerialized = new SerializedObject(debugView);
                Assert.That(debugSerialized.FindProperty("deckDebugText").objectReferenceValue, Is.Not.Null);
            }
            finally
            {
                if (closeWhenDone && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static ExplorationRegionData LoadRegion(string regionId)
        {
            string path = $"{RegionFolder}/{regionId}.asset";
            ExplorationRegionData region = AssetDatabase.LoadAssetAtPath<ExplorationRegionData>(path);
            Assert.That(region, Is.Not.Null, path);
            Assert.That(region.RegionId, Is.EqualTo(regionId), path);
            return region;
        }

        private static List<T> FindSceneComponents<T>(Scene scene) where T : Component
        {
            List<T> components = new List<T>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                components.AddRange(root.GetComponentsInChildren<T>(true));
            }

            return components;
        }

        private sealed class RegionExpectation
        {
            public RegionExpectation(
                string regionId,
                string displayName,
                ExplorationTier tier,
                DangerLevel danger,
                int zombieCount,
                float encounterChance,
                float heatPerExploration,
                float dailyHeatDecay)
            {
                RegionId = regionId;
                DisplayName = displayName;
                Tier = tier;
                Danger = danger;
                ZombieCount = zombieCount;
                EncounterChance = encounterChance;
                HeatPerExploration = heatPerExploration;
                DailyHeatDecay = dailyHeatDecay;
            }

            public string RegionId { get; }
            public string DisplayName { get; }
            public ExplorationTier Tier { get; }
            public DangerLevel Danger { get; }
            public int ZombieCount { get; }
            public float EncounterChance { get; }
            public float HeatPerExploration { get; }
            public float DailyHeatDecay { get; }
        }

        private sealed class PackExpectation
        {
            public PackExpectation(
                string regionId,
                string packId,
                RegionSupplyRarity rarity,
                int initialCount,
                int baseWeight)
            {
                RegionId = regionId;
                PackId = packId;
                Rarity = rarity;
                InitialCount = initialCount;
                BaseWeight = baseWeight;
            }

            public string RegionId { get; }
            public string PackId { get; }
            public RegionSupplyRarity Rarity { get; }
            public int InitialCount { get; }
            public int BaseWeight { get; }
        }
    }
}
