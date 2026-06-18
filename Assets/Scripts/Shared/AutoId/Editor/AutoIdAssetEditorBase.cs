using UnityEditor;
using UnityEngine;
using ZombieCardSurvive.Shared.AutoId;

namespace ZombieCardSurvive.Shared.AutoId.Editor
{
    public abstract class AutoIdAssetEditorBase : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            IAutoIdAsset autoIdAsset = target as IAutoIdAsset;
            string previousId = autoIdAsset?.Id;
            autoIdAsset?.EnsureId();
            if (autoIdAsset != null && previousId != autoIdAsset.Id)
            {
                EditorUtility.SetDirty(target);
            }

            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                using (new EditorGUI.DisabledScope(iterator.propertyPath == "m_Script" || IsIdProperty(iterator)))
                {
                    EditorGUILayout.PropertyField(iterator, true);
                }
            }

            EditorGUILayout.Space(8f);
            DrawIdTools(autoIdAsset);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawIdTools(IAutoIdAsset autoIdAsset)
        {
            if (autoIdAsset == null)
            {
                return;
            }

            EditorGUILayout.LabelField("ID \u5de5\u5177", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(autoIdAsset.Id, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));

            if (GUILayout.Button("\u91cd\u65b0\u751f\u6210 ID"))
            {
                Undo.RecordObject(target, "Regenerate Auto ID");
                autoIdAsset.RegenerateId();
                EditorUtility.SetDirty(target);
                serializedObject.Update();
            }
        }

        private static bool IsIdProperty(SerializedProperty property)
        {
            return property.propertyPath == "cardId" || property.propertyPath == "eventId";
        }
    }
}
