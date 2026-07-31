using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ZombieCardSurvive.Endings.Data;
using ZombieCardSurvive.Shared.AutoId.Editor;

namespace ZombieCardSurvive.Endings.Editor
{
    public abstract class EndingValidatedAssetEditor : AutoIdAssetEditorBase
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            IReadOnlyList<string> issues = GetValidationIssues();
            if (issues == null || issues.Count == 0)
            {
                EditorGUILayout.HelpBox("Data validation passed.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Data Validation", EditorStyles.boldLabel);
            for (int i = 0; i < issues.Count; i++)
            {
                EditorGUILayout.HelpBox(issues[i], MessageType.Warning);
            }
        }

        protected abstract IReadOnlyList<string> GetValidationIssues();
    }

    [CustomEditor(typeof(EndingScenarioData))]
    public sealed class EndingScenarioDataEditor : EndingValidatedAssetEditor
    {
        protected override IReadOnlyList<string> GetValidationIssues()
        {
            return ((EndingScenarioData)target).GetValidationIssues();
        }
    }

    [CustomEditor(typeof(EndingEventPackData))]
    public sealed class EndingEventPackDataEditor : EndingValidatedAssetEditor
    {
        protected override IReadOnlyList<string> GetValidationIssues()
        {
            return ((EndingEventPackData)target).GetValidationIssues();
        }
    }

    [CustomEditor(typeof(EndingKeyItemData))]
    public sealed class EndingKeyItemDataEditor : AutoIdAssetEditorBase
    {
    }
}
