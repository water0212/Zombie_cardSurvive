using System;
using System.Collections.Generic;
using ZombieCardSurvive.Effects.Data;

namespace ZombieCardSurvive.Systems
{
    public enum NextRoundEffectSourceKind
    {
        Zombie,
        Card,
        Event,
        Other
    }

    /// <summary>已鎖定的一次性下輪效果；建立後不再依來源是否存在而取消。</summary>
    public sealed class ScheduledNextRoundEffect
    {
        public ScheduledNextRoundEffect(
            NextRoundEffectData data,
            NextRoundEffectSourceKind sourceKind,
            string sourceId,
            int targetRound,
            int foodDemandModifier)
        {
            Data = data;
            SourceKind = sourceKind;
            SourceId = string.IsNullOrWhiteSpace(sourceId) ? sourceKind.ToString() : sourceId;
            TargetRound = Math.Max(1, targetRound);
            FoodDemandModifier = Math.Max(0, foodDemandModifier);
        }

        public NextRoundEffectData Data { get; }
        public NextRoundEffectSourceKind SourceKind { get; }
        public string SourceId { get; }
        public int TargetRound { get; }
        public int FoodDemandModifier { get; }
    }

    /// <summary>
    /// 下輪效果的單一 Runtime 權威。來源只負責建立排程；本系統負責鎖定、
    /// 到期啟用與移除，且不在啟用時重新查詢來源物件。
    /// </summary>
    public static class NextRoundEffectSystem
    {
        private static readonly List<ScheduledNextRoundEffect> pending =
            new List<ScheduledNextRoundEffect>();
        private static readonly List<ScheduledNextRoundEffect> lastActivated =
            new List<ScheduledNextRoundEffect>();

        private static int currentRound;
        private static int lastActivatedFoodDemandModifier;

        public static int CurrentRound => currentRound;
        public static IReadOnlyList<ScheduledNextRoundEffect> Pending => pending;
        public static IReadOnlyList<ScheduledNextRoundEffect> LastActivated => lastActivated;
        public static int LastActivatedFoodDemandModifier => lastActivatedFoodDemandModifier;

        public static void Reset(int roundIndex = 0)
        {
            currentRound = Math.Max(0, roundIndex);
            pending.Clear();
            lastActivated.Clear();
            lastActivatedFoodDemandModifier = 0;
        }

        public static bool TryScheduleForNextRound(
            NextRoundEffectData data,
            NextRoundEffectSourceKind sourceKind,
            string sourceId,
            out ScheduledNextRoundEffect scheduled)
        {
            return TryScheduleForRound(
                data,
                sourceKind,
                sourceId,
                currentRound + 1,
                out scheduled);
        }

        public static bool TryScheduleForRound(
            NextRoundEffectData data,
            NextRoundEffectSourceKind sourceKind,
            string sourceId,
            int targetRound,
            out ScheduledNextRoundEffect scheduled)
        {
            scheduled = null;
            if (data == null)
            {
                return false;
            }

            int foodDemandModifier = data.GetNextRoundFoodDemandModifier();
            if (foodDemandModifier <= 0)
            {
                return false;
            }

            scheduled = new ScheduledNextRoundEffect(
                data,
                sourceKind,
                sourceId,
                Math.Max(currentRound + 1, targetRound),
                foodDemandModifier);
            pending.Add(scheduled);
            return true;
        }

        /// <summary>啟用所有到期項目；漏掉的舊輪次也會安全補套用一次。</summary>
        public static void BeginRound(int roundIndex)
        {
            currentRound = Math.Max(0, roundIndex);
            lastActivated.Clear();
            lastActivatedFoodDemandModifier = 0;

            for (int index = pending.Count - 1; index >= 0; index--)
            {
                ScheduledNextRoundEffect scheduled = pending[index];
                if (scheduled == null || scheduled.TargetRound > currentRound)
                {
                    continue;
                }

                pending.RemoveAt(index);
                if (scheduled == null)
                {
                    continue;
                }

                FoodPlanSystem.AddTemporaryRoundConsumptionModifier(
                    scheduled.FoodDemandModifier);
                lastActivated.Add(scheduled);
                lastActivatedFoodDemandModifier = AddClamped(
                    lastActivatedFoodDemandModifier,
                    scheduled.FoodDemandModifier);
            }
        }

        private static int AddClamped(int left, int right)
        {
            long value = (long)left + right;
            return (int)Math.Max(0, Math.Min(int.MaxValue, value));
        }
    }
}
