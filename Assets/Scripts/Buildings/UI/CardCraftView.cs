using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZombieCardSurvive.Buildings.Data;
using ZombieCardSurvive.Buildings.Runtime;

namespace ZombieCardSurvive.Buildings.UI
{
    public class CardCraftView : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text productionText;
        [SerializeField] private Transform recipeRoot;
        [SerializeField] private Button recipeButtonPrefab;
        [SerializeField] private Transform queueRoot;
        [SerializeField] private CraftJobRowView jobRowPrefab;

        private readonly List<GameObject> generatedRecipeButtons = new List<GameObject>();
        private readonly List<CraftJobRowView> generatedJobRows = new List<CraftJobRowView>();
        private BuildingSystem system;
        private BuildingRuntime building;
        private CraftLineRuntime line;

        public void Bind(BuildingSystem owner, BuildingRuntime buildingRuntime, CraftLineRuntime lineRuntime)
        {
            system = owner;
            building = buildingRuntime;
            line = lineRuntime;
            Refresh();
        }

        public void Refresh()
        {
            if (line?.Data == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            if (titleText != null)
            {
                titleText.text = line.Data.DisplayName;
            }

            if (productionText != null)
            {
                int production = line.Data.BaseProduction + (building?.GetManufacturingBonus() ?? 0);
                productionText.text = $"製造力 {production}　佇列 {line.Jobs.Count}/{line.Data.QueueCapacity}";
            }

            RebuildRecipes();
            RebuildQueue();
        }

        private void RebuildRecipes()
        {
            DestroyGeneratedRecipes();
            if (recipeRoot == null || recipeButtonPrefab == null || line?.Data?.Recipes == null)
            {
                return;
            }

            foreach (CardCraftRecipeData recipe in line.Data.Recipes)
            {
                if (recipe == null)
                {
                    continue;
                }

                Button button = Instantiate(recipeButtonPrefab, recipeRoot);
                TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = $"{recipe.DisplayName}  需求 {recipe.RequiredProduction}  食物 {recipe.Cost.Food}  資源 {recipe.Cost.Resource}";
                }

                CardCraftRecipeData captured = recipe;
                button.interactable = system != null && system.CanSubmitActions && line.HasCapacity;
                button.onClick.AddListener(() => QueueRecipe(captured));
                generatedRecipeButtons.Add(button.gameObject);
            }
        }

        private void RebuildQueue()
        {
            DestroyGeneratedJobs();
            if (queueRoot == null || jobRowPrefab == null || line == null)
            {
                return;
            }

            foreach (CraftJobRuntime job in line.Jobs)
            {
                CraftJobRowView row = Instantiate(jobRowPrefab, queueRoot);
                row.Bind(system, building, line, job);
                generatedJobRows.Add(row);
            }
        }

        private void QueueRecipe(CardCraftRecipeData recipe)
        {
            if (building?.Data == null || line?.Data == null)
            {
                return;
            }

            system?.TryQueueCraft(building.Data.BuildingId, line.Data.InteractionId, recipe);
        }

        private void OnDestroy()
        {
            DestroyGeneratedRecipes();
            DestroyGeneratedJobs();
        }

        private void DestroyGeneratedRecipes()
        {
            foreach (GameObject item in generatedRecipeButtons)
            {
                if (item != null)
                {
                    Destroy(item);
                }
            }

            generatedRecipeButtons.Clear();
        }

        private void DestroyGeneratedJobs()
        {
            foreach (CraftJobRowView item in generatedJobRows)
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }

            generatedJobRows.Clear();
        }
    }
}
