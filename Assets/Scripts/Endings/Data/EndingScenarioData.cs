using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Events.Data;
using ZombieCardSurvive.Shared.AutoId;

namespace ZombieCardSurvive.Endings.Data
{
    [Serializable]
    public sealed class EndingOfferChoiceBinding
    {
        [Min(0)]
        [SerializeField] private int choiceIndex;
        [SerializeField] private EndingOfferChoiceResult result;

        public int ChoiceIndex => Mathf.Max(0, choiceIndex);
        public EndingOfferChoiceResult Result => result;
    }

    [CreateAssetMenu(fileName = "EndingScenario", menuName = "Zombie Card Survive/Endings/Ending Scenario")]
    public sealed class EndingScenarioData : ScriptableObject, IAutoIdAsset
    {
        [Header("Identity")]
        [SerializeField] private string endingScenarioId;
        [SerializeField] private string displayName;
        [TextArea]
        [SerializeField] private string description;
        [SerializeField] private Sprite artwork;
        [SerializeField] private EndingScenarioType scenarioType = EndingScenarioType.Special;

        [Header("Appearance Requirements")]
        [SerializeField] private EndingConditionGroup appearanceConditions = new EndingConditionGroup();

        [Header("Offer")]
        [Min(1)]
        [Tooltip("For a default ending, this is the forced start day.")]
        [SerializeField] private int offerDay = 1;
        [SerializeField] private EventData offerEvent;
        [Tooltip("Maps EventData choice indexes to Accept or Reject. Default endings may leave this empty.")]
        [SerializeField] private List<EndingOfferChoiceBinding> offerChoiceBindings = new List<EndingOfferChoiceBinding>();
        [SerializeField] private bool expiresWhenOfferDayIsMissed = true;

        [Header("Active Ending Event Pack")]
        [SerializeField] private EndingEventPackData activeEventPack;

        [Header("Deadline")]
        [SerializeField] private EndingDeadlineMode deadlineMode = EndingDeadlineMode.WhenActivePackExhausted;
        [Min(0)]
        [SerializeField] private int deadlineDaysAfterAcceptance;
        [SerializeField] private EventData deadlineEvent;

        [Header("Completion")]
        [SerializeField] private EndingConditionGroup completionConditions = new EndingConditionGroup();
        [SerializeField] private EndingEventPackData overdueEventPack;
        [SerializeField] private EventData completionEvent;

        [Header("Failure")]
        [SerializeField] private EndingFailureMode failureMode = EndingFailureMode.UseNormalRunFailure;
        [SerializeField] private EventData immediateFailureEvent;
        [Tooltip("Evaluator ID used when Failure Mode is Custom.")]
        [SerializeField] private string customFailureRuleId;

        [Header("Postgame")]
        [SerializeField] private bool canAppearAsPostgameEvent = true;
        [SerializeField] private EndingEventPackData postgameEventPack;

        public string EndingScenarioId => endingScenarioId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Artwork => artwork;
        public EndingScenarioType ScenarioType => scenarioType;
        public EndingConditionGroup AppearanceConditions => appearanceConditions;
        public int OfferDay => Mathf.Max(1, offerDay);
        public EventData OfferEvent => offerEvent;
        public IReadOnlyList<EndingOfferChoiceBinding> OfferChoiceBindings => offerChoiceBindings;
        public bool ExpiresWhenOfferDayIsMissed => expiresWhenOfferDayIsMissed;
        public EndingEventPackData ActiveEventPack => activeEventPack;
        public EndingDeadlineMode DeadlineMode => deadlineMode;
        public int DeadlineDaysAfterAcceptance => Mathf.Max(0, deadlineDaysAfterAcceptance);
        public EventData DeadlineEvent => deadlineEvent;
        public EndingConditionGroup CompletionConditions => completionConditions;
        public EndingEventPackData OverdueEventPack => overdueEventPack;
        public EventData CompletionEvent => completionEvent;
        public EndingFailureMode FailureMode => failureMode;
        public EventData ImmediateFailureEvent => immediateFailureEvent;
        public string CustomFailureRuleId => customFailureRuleId;
        public bool CanAppearAsPostgameEvent => canAppearAsPostgameEvent;
        public EndingEventPackData PostgameEventPack => postgameEventPack;
        public EndingEventPackData EffectivePostgameEventPack => postgameEventPack != null ? postgameEventPack : activeEventPack;
        public string Id => endingScenarioId;

        private void OnValidate()
        {
            EnsureId();
        }

        public void EnsureId()
        {
            if (string.IsNullOrWhiteSpace(endingScenarioId))
            {
                endingScenarioId = AutoIdUtility.CreateId("ending_scenario");
            }
        }

        public void RegenerateId()
        {
            endingScenarioId = AutoIdUtility.CreateId("ending_scenario");
        }

        public EndingOfferChoiceResult GetOfferChoiceResult(int choiceIndex)
        {
            if (offerChoiceBindings == null)
            {
                return EndingOfferChoiceResult.None;
            }

            for (int i = 0; i < offerChoiceBindings.Count; i++)
            {
                EndingOfferChoiceBinding binding = offerChoiceBindings[i];
                if (binding != null && binding.ChoiceIndex == choiceIndex)
                {
                    return binding.Result;
                }
            }

            return EndingOfferChoiceResult.None;
        }

        public List<string> GetValidationIssues()
        {
            List<string> issues = new List<string>();
            if (string.IsNullOrWhiteSpace(displayName))
            {
                issues.Add("Ending scenario display name is empty.");
            }

            if (scenarioType == EndingScenarioType.Special)
            {
                if (appearanceConditions == null || !appearanceConditions.HasConditions)
                {
                    issues.Add("A special ending should have at least one appearance condition.");
                }

                if (!HasOfferChoiceResult(EndingOfferChoiceResult.Accept))
                {
                    issues.Add("A special ending requires at least one Accept choice binding.");
                }

                if (!HasOfferChoiceResult(EndingOfferChoiceResult.Reject))
                {
                    issues.Add("A special ending requires at least one Reject choice binding.");
                }
            }

            AddConditionGroupIssues(appearanceConditions, "Appearance", issues);

            if (offerEvent == null)
            {
                issues.Add("Offer or forced-start event is missing.");
            }

            AddOfferChoiceBindingIssues(issues);

            if (activeEventPack == null)
            {
                issues.Add("Active ending event pack is missing.");
            }

            if (completionConditions == null || !completionConditions.HasConditions)
            {
                issues.Add("Ending completion conditions are empty.");
            }

            AddConditionGroupIssues(completionConditions, "Completion", issues);

            if (overdueEventPack == null)
            {
                issues.Add("Overdue event pack is missing.");
            }

            if (completionEvent == null)
            {
                issues.Add("Completion event is missing.");
            }

            if (deadlineMode == EndingDeadlineMode.DaysAfterAcceptance && deadlineDaysAfterAcceptance <= 0)
            {
                issues.Add("Days After Acceptance deadline requires a value greater than zero.");
            }

            if (deadlineMode == EndingDeadlineMode.AfterEvent && deadlineEvent == null)
            {
                issues.Add("After Event deadline requires a deadline event.");
            }

            if (failureMode == EndingFailureMode.Immediate && immediateFailureEvent == null)
            {
                issues.Add("Immediate failure mode requires a failure event.");
            }

            if (failureMode == EndingFailureMode.Custom && string.IsNullOrWhiteSpace(customFailureRuleId))
            {
                issues.Add("Custom failure mode requires a custom failure rule ID.");
            }

            return issues;
        }

        private bool HasOfferChoiceResult(EndingOfferChoiceResult result)
        {
            if (offerChoiceBindings == null)
            {
                return false;
            }

            for (int i = 0; i < offerChoiceBindings.Count; i++)
            {
                EndingOfferChoiceBinding binding = offerChoiceBindings[i];
                if (binding != null && binding.Result == result)
                {
                    return true;
                }
            }

            return false;
        }

        private static void AddConditionGroupIssues(EndingConditionGroup group, string groupName, List<string> issues)
        {
            if (group == null)
            {
                return;
            }

            List<string> groupIssues = group.GetValidationIssues();
            for (int i = 0; i < groupIssues.Count; i++)
            {
                issues.Add($"{groupName} {groupIssues[i]}");
            }
        }

        private void AddOfferChoiceBindingIssues(List<string> issues)
        {
            if (offerChoiceBindings == null || offerEvent == null)
            {
                return;
            }

            HashSet<int> mappedIndexes = new HashSet<int>();
            for (int i = 0; i < offerChoiceBindings.Count; i++)
            {
                EndingOfferChoiceBinding binding = offerChoiceBindings[i];
                if (binding == null)
                {
                    issues.Add($"Offer choice binding {i} is empty.");
                    continue;
                }

                if (!mappedIndexes.Add(binding.ChoiceIndex))
                {
                    issues.Add($"Offer choice index {binding.ChoiceIndex} is mapped more than once.");
                }

                if (binding.ChoiceIndex >= offerEvent.Choices.Count)
                {
                    issues.Add($"Offer choice index {binding.ChoiceIndex} is outside the offer event choice list.");
                }
            }
        }
    }
}
