using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Effects.Data;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Events.Runtime
{
    public sealed class EventChoiceExecutionPlan
    {
        private readonly ResolvedEventChoiceRuntime choice;
        private readonly GameEffectContext context;

        private EventChoiceExecutionPlan(
            ResolvedEventChoiceRuntime choice,
            GameEffectContext context,
            EventChoiceAvailability availability,
            int requiredFood,
            int requiredResource)
        {
            this.choice = choice;
            this.context = context;
            Availability = availability;
            RequiredFood = requiredFood;
            RequiredResource = requiredResource;
        }

        public EventChoiceAvailability Availability { get; }
        public int RequiredFood { get; }
        public int RequiredResource { get; }

        public static EventChoiceExecutionPlan Create(
            ResolvedEventChoiceRuntime choice,
            GameEffectContext context)
        {
            int availableFood = FoodSystem.AvailableFood;
            int availableResource = ResourceSystem.AvailableResource;
            if (choice == null)
            {
                return Invalid(
                    choice,
                    context,
                    availableFood,
                    availableResource,
                    "Resolved event choice is missing.");
            }

            int requiredFood = 0;
            int requiredResource = 0;
            for (int index = 0; index < choice.Effects.Count; index++)
            {
                ResolvedGameEffectRuntime effect = choice.Effects[index];
                if (effect == null || !effect.ShouldApply || !effect.HasInvalidConfiguration)
                {
                    continue;
                }

                return Invalid(
                    choice,
                    context,
                    availableFood,
                    availableResource,
                    string.IsNullOrWhiteSpace(effect.UnavailabilityReason)
                        ? $"Effect {index} has an invalid configuration."
                        : $"Effect {index}: {effect.UnavailabilityReason}");
            }

            for (int index = 0; index < choice.Effects.Count; index++)
            {
                ResolvedGameEffectRuntime effect = choice.Effects[index];
                if (effect == null || !effect.ShouldApply || effect.Source == null)
                {
                    continue;
                }

                if (effect.Source.IsResourceSpendEffect && !effect.Source.IsEventResourcePayment)
                {
                    return Invalid(
                        choice,
                        context,
                        availableFood,
                        availableResource,
                        $"Effect {index} spends a resource but is not configured with Event as its carrier.");
                }

                if (!effect.Source.IsChoiceCost)
                {
                    continue;
                }

                int amount = Mathf.Max(0, effect.FinalAmount);
                if (effect.Source.ValueEffectType == ValueEffectType.SpendFood)
                {
                    requiredFood = AddClamped(requiredFood, amount);
                }
                else if (effect.Source.ValueEffectType == ValueEffectType.SpendResource)
                {
                    requiredResource = AddClamped(requiredResource, amount);
                }
            }

            if (availableFood < requiredFood)
            {
                return Blocked(
                    choice,
                    context,
                    EventChoiceBlockReason.InsufficientPermanentFood,
                    requiredFood,
                    availableFood,
                    requiredResource,
                    availableResource);
            }

            if (availableResource < requiredResource)
            {
                return Blocked(
                    choice,
                    context,
                    EventChoiceBlockReason.InsufficientResource,
                    requiredFood,
                    availableFood,
                    requiredResource,
                    availableResource);
            }

            for (int index = 0; index < choice.Effects.Count; index++)
            {
                ResolvedGameEffectRuntime effect = choice.Effects[index];
                if (effect == null || !effect.ShouldApply || effect.Source == null
                    || effect.Source.IsChoiceCost || effect.Source.IsForcedLoss)
                {
                    continue;
                }

                if (!effect.CanApply(context))
                {
                    return Blocked(
                        choice,
                        context,
                        EventChoiceBlockReason.EffectUnavailable,
                        requiredFood,
                        availableFood,
                        requiredResource,
                        availableResource,
                        string.IsNullOrWhiteSpace(effect.UnavailabilityReason)
                            ? $"Effect {index} is unavailable."
                            : $"Effect {index}: {effect.UnavailabilityReason}");
                }
            }

            List<EventForcedLossResult> expectedLosses = SimulateForcedLosses(
                choice,
                availableFood - requiredFood,
                availableResource - requiredResource);

            for (int index = 0; index < choice.ForcedLossConsequences.Count; index++)
            {
                ResolvedForcedLossConsequenceRuntime consequence = choice.ForcedLossConsequences[index];
                if (!TryValidateConsequence(
                        choice,
                        consequence,
                        expectedLosses,
                        context,
                        out string failure))
                {
                    return Invalid(
                        choice,
                        context,
                        availableFood,
                        availableResource,
                        failure,
                        requiredFood,
                        requiredResource);
                }
            }

            EventChoiceAvailability availability = new EventChoiceAvailability(
                true,
                EventChoiceBlockReason.None,
                requiredFood,
                availableFood,
                requiredResource,
                availableResource);
            return new EventChoiceExecutionPlan(
                choice,
                context,
                availability,
                requiredFood,
                requiredResource);
        }

        public EventChoiceExecutionResult Execute()
        {
            if (!Availability.IsAvailable)
            {
                return EventChoiceExecutionResult.Failure(Availability);
            }

            int currentFood = FoodSystem.AvailableFood;
            int currentResource = ResourceSystem.AvailableResource;
            if (currentFood < RequiredFood || currentResource < RequiredResource)
            {
                EventChoiceAvailability changed = new EventChoiceAvailability(
                    false,
                    currentFood < RequiredFood
                        ? EventChoiceBlockReason.InsufficientPermanentFood
                        : EventChoiceBlockReason.InsufficientResource,
                    RequiredFood,
                    currentFood,
                    RequiredResource,
                    currentResource,
                    "Resources changed before the event choice was executed.");
                return EventChoiceExecutionResult.Failure(changed);
            }

            EventValueSnapshot valuesBefore = EventValueSnapshot.Capture();
            if (!FoodSystem.SpendFood(RequiredFood) || !ResourceSystem.SpendResource(RequiredResource))
            {
                Debug.LogError("Event choice costs failed after their aggregate availability was validated.");
                return EventChoiceExecutionResult.Failure(new EventChoiceAvailability(
                    false,
                    EventChoiceBlockReason.EffectUnavailable,
                    RequiredFood,
                    currentFood,
                    RequiredResource,
                    currentResource,
                    "Choice costs failed during execution."));
            }

            List<EventForcedLossResult> actualLosses = ConsumeForcedLosses(choice);
            List<ResolvedGameEffectRuntime> appliedEffects = new List<ResolvedGameEffectRuntime>();
            MoraleBatchRequest moraleBatch = new MoraleBatchRequest("EventChoice");

            for (int index = 0; index < choice.Effects.Count; index++)
            {
                ResolvedGameEffectRuntime effect = choice.Effects[index];
                if (effect == null || !effect.ShouldApply || effect.Source == null
                    || effect.Source.IsChoiceCost || effect.Source.IsForcedLoss)
                {
                    continue;
                }

                if (effect.IsMoraleEffect)
                {
                    moraleBatch.Add($"EventEffect[{index}]", effect.MoraleDelta);
                    appliedEffects.Add(effect);
                    continue;
                }

                if (!effect.TryApply(context))
                {
                    Debug.LogError($"Event effect {index} failed after its choice execution plan passed validation.");
                    EventChoiceAvailability failed = new EventChoiceAvailability(
                        false,
                        EventChoiceBlockReason.EffectUnavailable,
                        RequiredFood,
                        Availability.AvailableFood,
                        RequiredResource,
                        Availability.AvailableResource,
                        $"Effect {index} failed during execution.");
                    return EventChoiceExecutionResult.Failure(failed);
                }

                appliedEffects.Add(effect);
            }

            foreach (ResolvedForcedLossConsequenceRuntime consequence in choice.ForcedLossConsequences)
            {
                if (consequence?.Effect == null || !consequence.Effect.ShouldApply)
                {
                    continue;
                }

                EventForcedLossResult loss = FindForcedLoss(actualLosses, consequence.SourceEffectIndex);
                int amount = consequence.ResolveAmount(loss.Shortage);
                if (loss.Shortage <= 0)
                {
                    continue;
                }

                if (consequence.Effect.IsMoraleEffect)
                {
                    moraleBatch.Add(
                        $"ForcedLossConsequence[{consequence.SourceEffectIndex}]",
                        consequence.Effect.Source.ResolveMoraleDelta(amount));
                    continue;
                }

                if (!consequence.Effect.TryApply(context, amount))
                {
                    Debug.LogError(
                        $"Forced-loss consequence for effect {consequence.SourceEffectIndex} failed during execution.");
                }
            }

            MoraleBatchResult moraleSettlement = moraleBatch.Count > 0
                ? MoraleSystem.CommitBatch(moraleBatch)
                : null;

            string resultTitle = EventTextFormatter.ResolveExecution(choice.ResultTitle, actualLosses);
            string resultDescription = EventTextFormatter.ResolveExecution(choice.ResultDescription, actualLosses);
            return EventChoiceExecutionResult.Success(
                Availability,
                actualLosses,
                appliedEffects,
                resultTitle,
                resultDescription,
                moraleSettlement,
                EventValueSnapshot.Capture().DifferenceFrom(valuesBefore));
        }

        private static List<EventForcedLossResult> SimulateForcedLosses(
            ResolvedEventChoiceRuntime choice,
            int availableFood,
            int availableResource)
        {
            List<EventForcedLossResult> results = new List<EventForcedLossResult>();
            int food = Mathf.Max(0, availableFood);
            int resource = Mathf.Max(0, availableResource);

            for (int index = 0; index < choice.Effects.Count; index++)
            {
                ResolvedGameEffectRuntime effect = choice.Effects[index];
                if (effect == null || !effect.ShouldApply || effect.Source == null || !effect.Source.IsForcedLoss)
                {
                    continue;
                }

                int requested = Mathf.Max(0, effect.FinalAmount);
                int consumed;
                if (effect.Source.ValueEffectType == ValueEffectType.SpendFood)
                {
                    consumed = Mathf.Min(food, requested);
                    food -= consumed;
                }
                else
                {
                    consumed = Mathf.Min(resource, requested);
                    resource -= consumed;
                }

                results.Add(new EventForcedLossResult(
                    index,
                    effect.Source.ValueEffectType,
                    requested,
                    consumed,
                    requested - consumed));
            }

            return results;
        }

        private static List<EventForcedLossResult> ConsumeForcedLosses(ResolvedEventChoiceRuntime choice)
        {
            List<EventForcedLossResult> results = new List<EventForcedLossResult>();
            for (int index = 0; index < choice.Effects.Count; index++)
            {
                ResolvedGameEffectRuntime effect = choice.Effects[index];
                if (effect == null || !effect.ShouldApply || effect.Source == null || !effect.Source.IsForcedLoss)
                {
                    continue;
                }

                ValueConsumptionResult consumption =
                    effect.Source.ValueEffectType == ValueEffectType.SpendFood
                        ? FoodSystem.ConsumePermanentFoodAllowShortage(effect.FinalAmount)
                        : ResourceSystem.ConsumeResourceAllowShortage(effect.FinalAmount);
                results.Add(new EventForcedLossResult(
                    index,
                    effect.Source.ValueEffectType,
                    consumption.Requested,
                    consumption.Consumed,
                    consumption.Shortage));
            }

            return results;
        }

        private static bool TryValidateConsequence(
            ResolvedEventChoiceRuntime choice,
            ResolvedForcedLossConsequenceRuntime consequence,
            IReadOnlyList<EventForcedLossResult> losses,
            GameEffectContext context,
            out string failure)
        {
            failure = string.Empty;
            if (consequence?.Effect?.Source == null)
            {
                failure = "A forced-loss consequence effect is missing.";
                return false;
            }

            if (consequence.SourceEffectIndex < 0 || consequence.SourceEffectIndex >= choice.Effects.Count
                || choice.Effects[consequence.SourceEffectIndex]?.Source == null
                || !choice.Effects[consequence.SourceEffectIndex].Source.IsForcedLoss)
            {
                failure = $"Consequence source index {consequence.SourceEffectIndex} is not a ForcedLoss effect.";
                return false;
            }

            if (consequence.Effect.Source.IsResourceSpendEffect)
            {
                failure = "A forced-loss consequence cannot contain another food or resource payment.";
                return false;
            }

            EventForcedLossResult loss = FindForcedLoss(losses, consequence.SourceEffectIndex);
            int amount = consequence.ResolveAmount(loss.Shortage);
            if (loss.Shortage > 0 && consequence.Effect.ShouldApply
                && !consequence.Effect.CanApply(context, amount))
            {
                failure = $"Consequence for ForcedLoss effect {consequence.SourceEffectIndex} is unavailable.";
                return false;
            }

            return true;
        }

        private static EventForcedLossResult FindForcedLoss(
            IReadOnlyList<EventForcedLossResult> losses,
            int sourceEffectIndex)
        {
            if (losses != null)
            {
                for (int index = 0; index < losses.Count; index++)
                {
                    if (losses[index].SourceEffectIndex == sourceEffectIndex)
                    {
                        return losses[index];
                    }
                }
            }

            return default;
        }

        private static EventChoiceExecutionPlan Blocked(
            ResolvedEventChoiceRuntime choice,
            GameEffectContext context,
            EventChoiceBlockReason reason,
            int requiredFood,
            int availableFood,
            int requiredResource,
            int availableResource,
            string detail = "")
        {
            EventChoiceAvailability availability = new EventChoiceAvailability(
                false,
                reason,
                requiredFood,
                availableFood,
                requiredResource,
                availableResource,
                detail);
            return new EventChoiceExecutionPlan(
                choice,
                context,
                availability,
                requiredFood,
                requiredResource);
        }

        private static EventChoiceExecutionPlan Invalid(
            ResolvedEventChoiceRuntime choice,
            GameEffectContext context,
            int availableFood,
            int availableResource,
            string detail,
            int requiredFood = 0,
            int requiredResource = 0)
        {
            return Blocked(
                choice,
                context,
                EventChoiceBlockReason.InvalidConfiguration,
                requiredFood,
                availableFood,
                requiredResource,
                availableResource,
                detail);
        }

        private static int AddClamped(int left, int right)
        {
            long sum = (long)Math.Max(0, left) + Math.Max(0, right);
            return (int)Math.Min(int.MaxValue, sum);
        }
    }
}
