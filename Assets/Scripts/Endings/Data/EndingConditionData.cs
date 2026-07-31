using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZombieCardSurvive.Endings.Data
{
    [Serializable]
    public sealed class EndingConditionEntry
    {
        [SerializeField] private string displayName;
        [TextArea]
        [SerializeField] private string designerNote;
        [SerializeField] private EndingConditionType conditionType;
        [SerializeField] private EndingValueComparison comparison = EndingValueComparison.AtLeast;
        [Tooltip("Run flag, region, card, building, or custom evaluator ID.")]
        [SerializeField] private string targetId;
        [Tooltip("Used when Condition Type is Key Item Owned.")]
        [SerializeField] private EndingKeyItemData targetKeyItem;
        [SerializeField] private int requiredValue = 1;

        public string DisplayName => displayName;
        public string DesignerNote => designerNote;
        public EndingConditionType ConditionType => conditionType;
        public EndingValueComparison Comparison => comparison;
        public string TargetId => targetId;
        public EndingKeyItemData TargetKeyItem => targetKeyItem;
        public int RequiredValue => requiredValue;

        public string EffectiveTargetId => conditionType == EndingConditionType.KeyItemOwned && targetKeyItem != null
            ? targetKeyItem.KeyItemId
            : targetId;

        public List<string> GetValidationIssues()
        {
            List<string> issues = new List<string>();
            if (conditionType == EndingConditionType.KeyItemOwned && targetKeyItem == null)
            {
                issues.Add("Key Item Owned condition requires a target key item.");
            }

            if (RequiresTargetId(conditionType) && string.IsNullOrWhiteSpace(targetId))
            {
                issues.Add($"{conditionType} condition requires a target ID.");
            }

            return issues;
        }

        private static bool RequiresTargetId(EndingConditionType type)
        {
            switch (type)
            {
                case EndingConditionType.RunFlag:
                case EndingConditionType.RegionVisited:
                case EndingConditionType.CardAcquired:
                case EndingConditionType.BuildingLevel:
                case EndingConditionType.Custom:
                    return true;
                default:
                    return false;
            }
        }
    }

    [Serializable]
    public sealed class EndingConditionGroup
    {
        [SerializeField] private EndingConditionMatchMode matchMode = EndingConditionMatchMode.All;
        [SerializeField] private bool invertResult;
        [SerializeField] private List<EndingConditionEntry> conditions = new List<EndingConditionEntry>();

        public EndingConditionMatchMode MatchMode => matchMode;
        public bool InvertResult => invertResult;
        public IReadOnlyList<EndingConditionEntry> Conditions => conditions;
        public bool HasConditions => conditions != null && conditions.Count > 0;

        public List<string> GetValidationIssues()
        {
            List<string> issues = new List<string>();
            if (conditions == null)
            {
                return issues;
            }

            for (int i = 0; i < conditions.Count; i++)
            {
                EndingConditionEntry condition = conditions[i];
                if (condition == null)
                {
                    issues.Add($"Condition {i} is empty.");
                    continue;
                }

                List<string> conditionIssues = condition.GetValidationIssues();
                for (int j = 0; j < conditionIssues.Count; j++)
                {
                    issues.Add($"Condition {i}: {conditionIssues[j]}");
                }
            }

            return issues;
        }
    }
}
