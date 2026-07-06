using System.Collections.Generic;
using UnityEngine;

namespace ZombieCardSurvive.Buildings.Data
{
    [CreateAssetMenu(fileName = "CardCraftLine", menuName = "Zombie Card Survive/Buildings/Card Craft Line")]
    public class CardCraftInteractionData : BuildingInteractionData
    {
        [Min(0)]
        [SerializeField] private int baseProduction = 1;
        [Min(1)]
        [SerializeField] private int queueCapacity = 1;
        [SerializeField] private List<CardCraftRecipeData> recipes = new List<CardCraftRecipeData>();

        public int BaseProduction => Mathf.Max(0, baseProduction);
        public int QueueCapacity => Mathf.Max(1, queueCapacity);
        public IReadOnlyList<CardCraftRecipeData> Recipes => recipes;
    }
}
