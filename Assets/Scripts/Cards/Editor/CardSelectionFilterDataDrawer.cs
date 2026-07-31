using UnityEditor;
using UnityEngine;
using ZombieCardSurvive.Cards.Data;

namespace ZombieCardSurvive.Cards.Editor
{
    [CustomPropertyDrawer(typeof(CardSelectionFilterData))]
    public sealed class CardSelectionFilterDataDrawer : PropertyDrawer
    {
        private const float Spacing = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded)
            {
                return height;
            }

            SerializedProperty useType = property.FindPropertyRelative("useCardTypeFilter");
            SerializedProperty types = property.FindPropertyRelative("allowedCardTypes");
            SerializedProperty useRarity = property.FindPropertyRelative("useCardRarityFilter");
            SerializedProperty rarities = property.FindPropertyRelative("allowedCardRarities");
            SerializedProperty useCost = property.FindPropertyRelative("useEnergyCostFilter");

            height += Spacing + EditorGUIUtility.singleLineHeight;
            if (useType.boolValue)
            {
                height += Spacing + EditorGUI.GetPropertyHeight(types, true);
            }

            height += Spacing + EditorGUIUtility.singleLineHeight;
            if (useRarity.boolValue)
            {
                height += Spacing + EditorGUI.GetPropertyHeight(rarities, true);
            }

            height += Spacing + EditorGUIUtility.singleLineHeight;
            if (useCost.boolValue)
            {
                height += (Spacing + EditorGUIUtility.singleLineHeight) * 2f;
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, label, true);
            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            SerializedProperty useType = property.FindPropertyRelative("useCardTypeFilter");
            SerializedProperty types = property.FindPropertyRelative("allowedCardTypes");
            SerializedProperty useRarity = property.FindPropertyRelative("useCardRarityFilter");
            SerializedProperty rarities = property.FindPropertyRelative("allowedCardRarities");
            SerializedProperty useCost = property.FindPropertyRelative("useEnergyCostFilter");
            SerializedProperty costMode = property.FindPropertyRelative("energyCostFilterMode");
            SerializedProperty cost = property.FindPropertyRelative("energyCost");

            EditorGUI.indentLevel++;

            line.y += line.height + Spacing;
            EditorGUI.PropertyField(line, useType, new GUIContent("\u555f\u7528\u5361\u724c\u985e\u578b\u904e\u6ffe"));
            line.y += line.height + Spacing;
            if (useType.boolValue)
            {
                float fieldHeight = EditorGUI.GetPropertyHeight(types, true);
                line.height = fieldHeight;
                EditorGUI.PropertyField(line, types, new GUIContent("\u5141\u8a31\u7684\u5361\u724c\u985e\u578b"), true);
                line.y += fieldHeight + Spacing;
                line.height = EditorGUIUtility.singleLineHeight;
            }

            EditorGUI.PropertyField(line, useRarity, new GUIContent("\u555f\u7528\u7a00\u6709\u5ea6\u904e\u6ffe"));
            line.y += line.height + Spacing;
            if (useRarity.boolValue)
            {
                float fieldHeight = EditorGUI.GetPropertyHeight(rarities, true);
                line.height = fieldHeight;
                EditorGUI.PropertyField(line, rarities, new GUIContent("\u5141\u8a31\u7684\u7a00\u6709\u5ea6"), true);
                line.y += fieldHeight + Spacing;
                line.height = EditorGUIUtility.singleLineHeight;
            }

            EditorGUI.PropertyField(line, useCost, new GUIContent("\u555f\u7528\u8cbb\u7528\u904e\u6ffe"));
            if (useCost.boolValue)
            {
                line.y += line.height + Spacing;
                EditorGUI.PropertyField(line, costMode, new GUIContent("\u6bd4\u8f03\u65b9\u5f0f"));
                line.y += line.height + Spacing;
                cost.intValue = Mathf.Max(0, EditorGUI.IntField(line, new GUIContent("\u57fa\u6e96\u8cbb\u7528"), cost.intValue));
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }
    }
}
