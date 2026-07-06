using UnityEngine;
using ZombieCardSurvive.Cards.Data;
using ZombieCardSurvive.Shared.AutoId;

namespace ZombieCardSurvive.Buildings.Data
{
    [CreateAssetMenu(fileName = "CardCraftRecipe", menuName = "Zombie Card Survive/Buildings/Card Craft Recipe")]
    public class CardCraftRecipeData : ScriptableObject, IAutoIdAsset
    {
        [SerializeField] private string recipeId;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite artwork;
        [SerializeField] private CardBase outputCard;
        [Min(1)]
        [SerializeField] private int outputCount = 1;
        [SerializeField] private ResourceCost cost;
        [Min(1)]
        [SerializeField] private int requiredProduction = 1;

        public string RecipeId => recipeId;
        public string DisplayName => displayName;
        public Sprite Artwork => artwork;
        public CardBase OutputCard => outputCard;
        public int OutputCount => Mathf.Max(1, outputCount);
        public ResourceCost Cost => cost;
        public int RequiredProduction => Mathf.Max(1, requiredProduction);
        public string Id => recipeId;

        private void OnValidate()
        {
            EnsureId();
        }

        public void EnsureId()
        {
            if (string.IsNullOrWhiteSpace(recipeId))
            {
                recipeId = AutoIdUtility.CreateId("craft_recipe");
            }
        }

        public void RegenerateId()
        {
            recipeId = AutoIdUtility.CreateId("craft_recipe");
        }
    }
}
