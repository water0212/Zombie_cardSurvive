#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using ZombieCardSurvive.Buildings.Data;
using ZombieCardSurvive.Cards.Data;
using ZombieCardSurvive.Effects.Data;

namespace ZombieCardSurvive.Effects.Editor
{
    public static class TemporaryFoodV1SetupUtility
    {
        private const string CookingCardPath = "Assets/ScriptObj/CardScriptObj/NewCard/烹飪.asset";
        private const string CookingRecipePath = "Assets/ScriptObj/BuildingScriptObj/KItchen/KitchenCardCraftRecipe.asset";

        [MenuItem("Tools/Zombie Card Survive/Setup Temporary Food V1")]
        public static void Setup()
        {
            CardBase cookingCard = AssetDatabase.LoadAssetAtPath<CardBase>(CookingCardPath);
            if (cookingCard == null)
            {
                Debug.LogWarning($"Temporary Food V1 setup could not find the cooking card at '{CookingCardPath}'.");
                return;
            }

            ConfigureCookingCard(cookingCard);
            ConfigureCookingRecipe(cookingCard);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Temporary Food V1 setup complete. Cooking now spends 2 permanent food and adds 6 temporary food.");
        }

        private static void ConfigureCookingCard(CardBase cookingCard)
        {
            SerializedObject serialized = new SerializedObject(cookingCard);
            serialized.FindProperty("description").stringValue = "花費2點永久食物，獲得6點臨時食物";
            serialized.FindProperty("cardType").enumValueIndex = (int)CardType.Food;
            serialized.FindProperty("hasLimitedUses").boolValue = true;
            serialized.FindProperty("maxUsesPerRun").intValue = 3;

            SerializedProperty costs = serialized.FindProperty("costs");
            costs.arraySize = 1;
            ConfigureEffect(costs.GetArrayElementAtIndex(0), ValueEffectType.SpendFood, 2);

            SerializedProperty effects = serialized.FindProperty("effects");
            effects.arraySize = 1;
            ConfigureEffect(effects.GetArrayElementAtIndex(0), ValueEffectType.AddTemporaryFood, 6);

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(cookingCard);
        }

        private static void ConfigureCookingRecipe(CardBase cookingCard)
        {
            CardCraftRecipeData recipe = AssetDatabase.LoadAssetAtPath<CardCraftRecipeData>(CookingRecipePath);
            if (recipe == null)
            {
                Debug.LogWarning($"Temporary Food V1 setup could not find the cooking recipe at '{CookingRecipePath}'.");
                return;
            }

            SerializedObject serialized = new SerializedObject(recipe);
            serialized.FindProperty("outputCard").objectReferenceValue = cookingCard;
            serialized.FindProperty("outputCount").intValue = 2;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(recipe);
        }

        private static void ConfigureEffect(SerializedProperty effect, ValueEffectType type, int amount)
        {
            effect.FindPropertyRelative("carrier").enumValueIndex = (int)GameEffectCarrier.Card;
            effect.FindPropertyRelative("category").enumValueIndex = (int)GameEffectCategory.Value;
            effect.FindPropertyRelative("valueEffectType").enumValueIndex = (int)type;
            effect.FindPropertyRelative("cardFlowEffectType").enumValueIndex = 0;
            effect.FindPropertyRelative("buildingEffectType").enumValueIndex = 0;
            effect.FindPropertyRelative("survivorEffectType").enumValueIndex = 0;
            effect.FindPropertyRelative("amount").intValue = amount;
            effect.FindPropertyRelative("effectId").stringValue = string.Empty;
            effect.FindPropertyRelative("isPermanent").boolValue = false;
            effect.FindPropertyRelative("remainingTurns").intValue = 0;
        }
    }
}
#endif
