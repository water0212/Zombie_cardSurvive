using System;
using UnityEditor;
using UnityEngine;
using ZombieCardSurvive.Effects.Data;

namespace ZombieCardSurvive.Effects.Editor
{
    [CustomPropertyDrawer(typeof(GameEffectData))]
    public class GameEffectDataDrawer : PropertyDrawer
    {
        private const float LineHeight = 18f;
        private const float LineSpacing = 4f;
        private const float BoxPadding = 6f;

        private static readonly GameEffectCarrier[] CarrierValues =
        {
            GameEffectCarrier.Card,
            GameEffectCarrier.Event,
            GameEffectCarrier.Other
        };

        private static readonly string[] CarrierLabels =
        {
            "\u5361\u724c",
            "\u4e8b\u4ef6",
            "\u5176\u4ed6"
        };

        private static readonly GameEffectCategory[] CardCategoryValues =
        {
            GameEffectCategory.Value,
            GameEffectCategory.CardFlow
        };

        private static readonly string[] CardCategoryLabels =
        {
            "\u6578\u503c\u6548\u679c",
            "\u5361\u724c\u6d41\u7a0b"
        };

        private static readonly GameEffectCategory[] ValueOnlyCategoryValues =
        {
            GameEffectCategory.Value
        };

        private static readonly string[] ValueOnlyCategoryLabels =
        {
            "\u6578\u503c\u6548\u679c"
        };

        private static readonly ValueEffectType[] ValueEffectValues =
        {
            ValueEffectType.AddFood,
            ValueEffectType.SpendFood,
            ValueEffectType.AddFoodProduction,
            ValueEffectType.AddResource,
            ValueEffectType.SpendResource,
            ValueEffectType.AddResourceProduction,
            ValueEffectType.AddZombieThreat,
            ValueEffectType.RemoveZombieThreat,
            ValueEffectType.AddPendingDamage,
            ValueEffectType.ClearPendingDamage,
            ValueEffectType.AddCurrentEnergy,
            ValueEffectType.IncreaseMaxEnergy,
            ValueEffectType.DecreaseMaxEnergy,
            ValueEffectType.RefillEnergy,
            ValueEffectType.AddMorale,
            ValueEffectType.ReduceMorale,
            ValueEffectType.AddTemporaryFood,
            ValueEffectType.AddRoundFoodConsumptionModifier
        };

        private static readonly string[] ValueEffectLabels =
        {
            "\u589e\u52a0\u98df\u7269",
            "\u6d88\u8017\u98df\u7269",
            "\u6bcf\u56de\u5408\u98df\u7269\u7522\u51fa",
            "\u589e\u52a0\u8cc7\u6e90",
            "\u6d88\u8017\u8cc7\u6e90",
            "\u6bcf\u56de\u5408\u8cc7\u6e90\u7522\u51fa",
            "\u589e\u52a0\u6bad\u5c4d\u5a01\u8105",
            "\u79fb\u9664\u6bad\u5c4d\u5a01\u8105",
            "\u589e\u52a0\u6230\u9b25\u503c",
            "\u6e05\u9664\u6230\u9b25\u503c",
            "\u589e\u52a0\u76ee\u524d\u80fd\u91cf",
            "\u589e\u52a0\u6700\u5927\u80fd\u91cf",
            "\u6e1b\u5c11\u6700\u5927\u80fd\u91cf",
            "\u80fd\u91cf\u56de\u6eff",
            "\u589e\u52a0\u58eb\u6c23",
            "\u964d\u4f4e\u58eb\u6c23",
            "\u589e\u52a0\u81e8\u6642\u98df\u7269",
            "\u672c\u8f2a\u56fa\u5b9a\u98df\u7269\u6d88\u8017\u4fee\u6b63"
        };

        private static readonly CardFlowEffectType[] CardFlowEffectValues =
        {
            CardFlowEffectType.DrawCards,
            CardFlowEffectType.GrantReplacementChance,
            CardFlowEffectType.AddCardToDiscardPile,
            CardFlowEffectType.ReduceOtherHandEnergyCosts,
            CardFlowEffectType.ReduceDrawPileEnergyCosts
        };

        private static readonly string[] CardFlowEffectLabels =
        {
            "\u62bd\u724c",
            "\u7372\u5f97\u66ff\u63db\u6a5f\u6703",
            "\u52a0\u5165\u5361\u724c\u5230\u68c4\u724c\u5806",
            "\u964d\u4f4e\u5176\u4ed6\u624b\u724c\u8cbb\u7528",
            "\u9078\u64c7\u62bd\u724c\u5806\u5361\u724c\u964d\u4f4e\u8cbb\u7528"
        };

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty categoryProperty = property.FindPropertyRelative("category");
            GameEffectCategory category = (GameEffectCategory)categoryProperty.enumValueIndex;
            int lineCount = 4;

            if (ShouldShowProductionFields(property, category))
            {
                lineCount += 3;
            }

            if (ShouldShowCardDataField(property, category))
            {
                lineCount += 1;
            }

            if (ShouldShowSelectionCountField(property, category))
            {
                lineCount += 1;
            }

            lineCount += 4;
            float height = BoxPadding * 2f + lineCount * LineHeight + (lineCount - 1) * LineSpacing;
            if (ShouldShowSelectionFilterField(property, category))
            {
                SerializedProperty filterProperty = property.FindPropertyRelative("cardSelectionFilter");
                height += EditorGUI.GetPropertyHeight(filterProperty, true) + LineSpacing;
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            GUI.Box(position, GUIContent.none);

            Rect line = new Rect(position.x + BoxPadding, position.y + BoxPadding, position.width - BoxPadding * 2f, LineHeight);

            SerializedProperty carrierProperty = property.FindPropertyRelative("carrier");
            SerializedProperty categoryProperty = property.FindPropertyRelative("category");
            SerializedProperty valueEffectProperty = property.FindPropertyRelative("valueEffectType");
            SerializedProperty cardFlowEffectProperty = property.FindPropertyRelative("cardFlowEffectType");
            SerializedProperty cardDataProperty = property.FindPropertyRelative("cardData");
            SerializedProperty amountProperty = property.FindPropertyRelative("amount");
            SerializedProperty selectionCountProperty = property.FindPropertyRelative("selectionCount");
            SerializedProperty cardSelectionFilterProperty = property.FindPropertyRelative("cardSelectionFilter");
            SerializedProperty effectIdProperty = property.FindPropertyRelative("effectId");
            SerializedProperty isPermanentProperty = property.FindPropertyRelative("isPermanent");
            SerializedProperty remainingTurnsProperty = property.FindPropertyRelative("remainingTurns");
            SerializedProperty requiredBuildingIdProperty = property.FindPropertyRelative("requiredBuildingId");
            SerializedProperty requiredBuildingLevelProperty = property.FindPropertyRelative("requiredBuildingLevel");
            SerializedProperty invertBuildingRequirementProperty = property.FindPropertyRelative("invertBuildingRequirement");

            GameEffectCarrier carrier = DrawMappedEnum(line, "\u627f\u8f09\u8005", carrierProperty, CarrierValues, CarrierLabels);
            line.y += LineHeight + LineSpacing;

            GameEffectCategory[] categoryValues = GetCategoryValues(carrier);
            string[] categoryLabels = GetCategoryLabels(carrier);
            GameEffectCategory category = DrawMappedEnum(line, "\u6548\u679c\u5206\u985e", categoryProperty, categoryValues, categoryLabels);
            line.y += LineHeight + LineSpacing;

            if (category == GameEffectCategory.Value)
            {
                DrawMappedEnum(line, "\u6548\u679c\u985e\u578b", valueEffectProperty, ValueEffectValues, ValueEffectLabels);
            }
            else if (category == GameEffectCategory.CardFlow)
            {
                DrawMappedEnum(line, "\u6548\u679c\u985e\u578b", cardFlowEffectProperty, CardFlowEffectValues, CardFlowEffectLabels);
            }
            else
            {
                EditorGUI.LabelField(line, "\u6548\u679c\u985e\u578b", "\u5c1a\u672a\u5be6\u4f5c");
            }

            line.y += LineHeight + LineSpacing;

            EditorGUI.PropertyField(line, amountProperty, new GUIContent("\u6578\u503c"));
            line.y += LineHeight + LineSpacing;

            if (ShouldShowSelectionCountField(property, category))
            {
                selectionCountProperty.intValue = Mathf.Max(
                    1,
                    EditorGUI.IntField(line, new GUIContent("\u9078\u64c7\u6578\u91cf"), selectionCountProperty.intValue));
                line.y += LineHeight + LineSpacing;
            }

            if (ShouldShowSelectionFilterField(property, category))
            {
                float filterHeight = EditorGUI.GetPropertyHeight(cardSelectionFilterProperty, true);
                line.height = filterHeight;
                EditorGUI.PropertyField(
                    line,
                    cardSelectionFilterProperty,
                    new GUIContent("\u9078\u724c\u904e\u6ffe"),
                    true);
                line.y += filterHeight + LineSpacing;
                line.height = LineHeight;
            }

            if (ShouldShowProductionFields(property, category))
            {
                EditorGUI.PropertyField(line, effectIdProperty, new GUIContent("\u6548\u679c ID"));
                line.y += LineHeight + LineSpacing;
                EditorGUI.PropertyField(line, isPermanentProperty, new GUIContent("\u6c38\u4e45\u6548\u679c"));
                line.y += LineHeight + LineSpacing;
                EditorGUI.PropertyField(line, remainingTurnsProperty, new GUIContent("\u6301\u7e8c\u56de\u5408"));
                line.y += LineHeight + LineSpacing;
            }

            if (ShouldShowCardDataField(property, category))
            {
                EditorGUI.PropertyField(line, cardDataProperty, new GUIContent("\u5361\u724c"));
                line.y += LineHeight + LineSpacing;
            }

            EditorGUI.LabelField(line, "\u5efa\u7bc9\u689d\u4ef6\uff08\u7559\u7a7a = \u4e0d\u9650\u5236\uff09", EditorStyles.boldLabel);
            line.y += LineHeight + LineSpacing;

            requiredBuildingIdProperty.stringValue = EditorGUI.TextField(
                line,
                new GUIContent("\u5efa\u7bc9 ID"),
                requiredBuildingIdProperty.stringValue);
            line.y += LineHeight + LineSpacing;

            requiredBuildingLevelProperty.intValue = Mathf.Max(
                0,
                EditorGUI.IntField(line, new GUIContent("\u6700\u4f4e\u7b49\u7d1a"), requiredBuildingLevelProperty.intValue));
            line.y += LineHeight + LineSpacing;

            invertBuildingRequirementProperty.boolValue = EditorGUI.Toggle(
                line,
                new GUIContent("\u689d\u4ef6\u53cd\u8f49"),
                invertBuildingRequirementProperty.boolValue);

            EditorGUI.EndProperty();
        }

        private static GameEffectCategory[] GetCategoryValues(GameEffectCarrier carrier)
        {
            return carrier == GameEffectCarrier.Card || carrier == GameEffectCarrier.Event ? CardCategoryValues : ValueOnlyCategoryValues;
        }

        private static string[] GetCategoryLabels(GameEffectCarrier carrier)
        {
            return carrier == GameEffectCarrier.Card || carrier == GameEffectCarrier.Event ? CardCategoryLabels : ValueOnlyCategoryLabels;
        }

        private static bool ShouldShowProductionFields(SerializedProperty property, GameEffectCategory category)
        {
            if (category != GameEffectCategory.Value)
            {
                return false;
            }

            SerializedProperty valueEffectProperty = property.FindPropertyRelative("valueEffectType");
            ValueEffectType valueEffectType = (ValueEffectType)valueEffectProperty.enumValueIndex;
            return valueEffectType == ValueEffectType.AddFoodProduction || valueEffectType == ValueEffectType.AddResourceProduction;
        }

        private static bool ShouldShowCardDataField(SerializedProperty property, GameEffectCategory category)
        {
            if (category != GameEffectCategory.CardFlow)
            {
                return false;
            }

            SerializedProperty cardFlowEffectProperty = property.FindPropertyRelative("cardFlowEffectType");
            CardFlowEffectType cardFlowEffectType = (CardFlowEffectType)cardFlowEffectProperty.enumValueIndex;
            return cardFlowEffectType == CardFlowEffectType.AddCardToDiscardPile;
        }

        private static bool ShouldShowSelectionCountField(SerializedProperty property, GameEffectCategory category)
        {
            return GetCardFlowEffectType(property, category) == CardFlowEffectType.ReduceDrawPileEnergyCosts;
        }

        private static bool ShouldShowSelectionFilterField(SerializedProperty property, GameEffectCategory category)
        {
            CardFlowEffectType? effectType = GetCardFlowEffectType(property, category);
            return effectType == CardFlowEffectType.ReduceOtherHandEnergyCosts
                || effectType == CardFlowEffectType.ReduceDrawPileEnergyCosts;
        }

        private static CardFlowEffectType? GetCardFlowEffectType(SerializedProperty property, GameEffectCategory category)
        {
            if (category != GameEffectCategory.CardFlow)
            {
                return null;
            }

            SerializedProperty cardFlowEffectProperty = property.FindPropertyRelative("cardFlowEffectType");
            return (CardFlowEffectType)cardFlowEffectProperty.enumValueIndex;
        }

        private static T DrawMappedEnum<T>(Rect rect, string label, SerializedProperty property, T[] values, string[] labels)
            where T : Enum
        {
            T currentValue = (T)Enum.ToObject(typeof(T), property.enumValueIndex);
            int selectedIndex = IndexOf(values, currentValue);
            if (selectedIndex < 0)
            {
                selectedIndex = 0;
                property.enumValueIndex = Convert.ToInt32(values[0]);
            }

            selectedIndex = EditorGUI.Popup(rect, label, selectedIndex, labels);
            T selectedValue = values[Mathf.Clamp(selectedIndex, 0, values.Length - 1)];
            property.enumValueIndex = Convert.ToInt32(selectedValue);
            return selectedValue;
        }

        private static int IndexOf<T>(T[] values, T value)
            where T : Enum
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (Equals(values[i], value))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
