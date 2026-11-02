using ZombieCardSurvive.Systems.Transactions;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZombieCardSurvive.Systems
{
    public static class MoraleSystem
    {
        public const int InitialMorale = 100;
        public const int AbsoluteMaximumMorale = 100;
        public const int DefaultStableThreshold = 70;
        public const int DefaultCriticalThreshold = 35;
        public const int DefaultHighMoraleDecayPerRound = 3;

        private static int morale = InitialMorale;
        private static int defaultMorale = 70;
        private static int maxMorale = AbsoluteMaximumMorale;
        private static int highMoraleDecayPerRound = DefaultHighMoraleDecayPerRound;
        private static int stableThreshold = DefaultStableThreshold;
        private static int criticalThreshold = DefaultCriticalThreshold;
        private static MoraleCollapseStage collapseStage = MoraleCollapseStage.Stable;
        private static bool isCollapsed;

        public static event Action<MoraleChangeResult> StateChanged;
        public static event Action<MoraleStageTransition, MoraleChangeResult> StageChanged;
        public static event Action<MoraleChangeResult> BaseCollapsed;

        public static int Morale => morale;
        public static int CurrentMorale => morale;
        public static int DefaultMorale => defaultMorale;
        public static int MaxMorale => maxMorale;
        public static int MaximumMorale => maxMorale;
        public static int HighMoraleDecayPerRound => highMoraleDecayPerRound;
        public static int StableThreshold => stableThreshold;
        public static int CriticalThreshold => criticalThreshold;
        public static MoraleCollapseStage CollapseStage => collapseStage;
        public static bool IsCollapsed => isCollapsed;

        public static void Reset(
            int startingMorale = InitialMorale,
            int defaultValue = 70,
            int maxValue = AbsoluteMaximumMorale,
            int highDecayPerRound = DefaultHighMoraleDecayPerRound,
            int stableStageThreshold = DefaultStableThreshold,
            int criticalStageThreshold = DefaultCriticalThreshold)
        {
            int moraleBefore = morale;
            int maximumBefore = maxMorale;
            MoraleCollapseStage stageBefore = collapseStage;
            bool wasCollapsed = isCollapsed;

            maxMorale = Mathf.Clamp(maxValue, 0, AbsoluteMaximumMorale);
            defaultMorale = Mathf.Clamp(defaultValue, 0, maxMorale);
            morale = Mathf.Clamp(startingMorale, 0, maxMorale);
            highMoraleDecayPerRound = Mathf.Max(0, highDecayPerRound);
            stableThreshold = Mathf.Clamp(stableStageThreshold, 0, AbsoluteMaximumMorale);
            criticalThreshold = Mathf.Clamp(criticalStageThreshold, 0, stableThreshold);
            collapseStage = MoraleCollapseStage.Stable;
            isCollapsed = false;

            StateChanged?.Invoke(new MoraleChangeResult(
                "Reset",
                moraleBefore,
                morale,
                maximumBefore,
                maxMorale,
                morale - moraleBefore,
                maxMorale - maximumBefore,
                stageBefore,
                collapseStage,
                Array.Empty<MoraleStageTransition>(),
                wasCollapsed,
                isCollapsed,
                false));
        }

        /// <summary>No run exists in the menu. Do not create a new 100/100 run as cleanup.</summary>
        public static void ClearRunState()
        {
            Reset(0, 0, 0, 0);
        }

        public static int GetMorale()
        {
            return morale;
        }

        public static void AddMorale(int amount)
        {
            CommitChange("AddMorale", Mathf.Max(0, amount));
        }

        public static void ReduceMorale(int amount)
        {
            CommitChange("ReduceMorale", -Mathf.Max(0, amount));
        }

        public static void ChangeMorale(int amount)
        {
            CommitChange("ChangeMorale", amount);
        }

        /// <summary>
        /// 將完整交易中的逐項來源合併後只提交一次，避免效果排列順序造成中途降階。
        /// </summary>
        public static MoraleBatchResult CommitBatch(MoraleBatchRequest request)
        {
            if (ValueTransactions.Executing != null)
            {
                var result = CommitBatchCore(request);
                ValueTransactions.Executing.AttachMorale(result);
                return result;
            }
            using (var transaction = new ValueTransaction(request?.Source ?? "Morale", ValueTransactionKind.System))
            {
                var result = transaction.Execute(() => CommitBatchCore(request));
                transaction.AttachMorale(result);
                transaction.Complete();
                return result;
            }
        }
        private static MoraleBatchResult CommitBatchCore(MoraleBatchRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            MoraleChangeResult changeResult = CommitChange(
                request.Source,
                request.NetMoraleDelta,
                request.NetMaximumMoraleDelta);
            return new MoraleBatchResult(
                request.Source,
                new List<MoraleBatchContribution>(request.Contributions).AsReadOnly(),
                request.NetMoraleDelta,
                request.NetMaximumMoraleDelta,
                changeResult);
        }

        /// <summary>
        /// 原子提交一次士氣與永久上限變化：先改上限並限制目前值，再套用目前士氣變化。
        /// 已崩潰的 Run 不再接受後續變化。
        /// </summary>
        public static MoraleChangeResult CommitChange(
            string source,
            int moraleDelta = 0,
            int maximumMoraleDelta = 0)
        {
            if (ValueTransactions.Executing != null) return CommitChangeCore(source, moraleDelta, maximumMoraleDelta);
            using (var transaction = new ValueTransaction(source, ValueTransactionKind.System))
            {
                var result = transaction.Execute(() => CommitChangeCore(source, moraleDelta, maximumMoraleDelta));
                transaction.Complete();
                return result;
            }
        }
        private static MoraleChangeResult CommitChangeCore(
            string source,
            int moraleDelta = 0,
            int maximumMoraleDelta = 0)
        {
            int moraleBefore = morale;
            int maximumBefore = maxMorale;
            MoraleCollapseStage stageBefore = collapseStage;
            bool wasCollapsed = isCollapsed;

            if (!isCollapsed)
            {
                MoraleChangeResult preview = PreviewChange(source, moraleDelta, maximumMoraleDelta);
                maxMorale = preview.MaximumMoraleAfter;
                morale = preview.MoraleAfter;
                collapseStage = preview.StageAfter;
                isCollapsed = morale == 0;
            }

            IReadOnlyList<MoraleStageTransition> transitions = BuildTransitions(stageBefore, collapseStage);
            MoraleChangeResult result = new MoraleChangeResult(
                source,
                moraleBefore,
                morale,
                maximumBefore,
                maxMorale,
                moraleDelta,
                maximumMoraleDelta,
                stageBefore,
                collapseStage,
                transitions,
                wasCollapsed,
                isCollapsed,
                !wasCollapsed && isCollapsed);

            if (!wasCollapsed)
            {
                StateChanged?.Invoke(result);
                foreach (MoraleStageTransition transition in transitions)
                {
                    StageChanged?.Invoke(transition, result);
                }

                if (result.DidCollapse)
                {
                    BaseCollapsed?.Invoke(result);
                }
            }

            return result;
        }

        /// <summary>以相同提交規則計算結果，但不修改狀態或發送通知。</summary>
        public static MoraleChangeResult PreviewChange(string source, int moraleDelta = 0, int maximumMoraleDelta = 0)
        {
            int maximum = isCollapsed ? maxMorale : AddClamped(maxMorale, maximumMoraleDelta, 0, AbsoluteMaximumMorale);
            int current = isCollapsed ? morale : AddClamped(Math.Min(morale, maximum), moraleDelta, 0, maximum);
            MoraleCollapseStage stage = isCollapsed ? collapseStage : CalculateStage(collapseStage, current);
            bool collapsed = isCollapsed || current == 0;
            return new MoraleChangeResult(source, morale, current, maxMorale, maximum,
                moraleDelta, maximumMoraleDelta, collapseStage, stage,
                BuildTransitions(collapseStage, stage), isCollapsed, collapsed, !isCollapsed && collapsed);
        }

        /// <summary>以完整交易的淨變化計算高士氣自然回落量。</summary>
        public static int PreviewRoundEndDrift(int moraleDelta, int maximumMoraleDelta)
        {
            int value = PreviewChange("Preview", moraleDelta, maximumMoraleDelta).MoraleAfter;
            return value > defaultMorale ? -Math.Min(highMoraleDecayPerRound, value - defaultMorale) : 0;
        }

        public static int ApplyRoundEndDrift()
        {
            if (morale <= defaultMorale || highMoraleDecayPerRound <= 0)
            {
                return 0;
            }

            int before = morale;
            int target = Mathf.Max(defaultMorale, morale - highMoraleDecayPerRound);
            MoraleChangeResult result = CommitChange("HighMoraleDrift", target - morale);
            return result.MoraleAfter - before;
        }

        private static MoraleCollapseStage CalculateStage(
            MoraleCollapseStage currentStage,
            int currentMorale)
        {
            MoraleCollapseStage requiredStage = MoraleCollapseStage.Stable;
            if (currentMorale < criticalThreshold)
            {
                requiredStage = MoraleCollapseStage.Brink;
            }
            else if (currentMorale < stableThreshold)
            {
                requiredStage = MoraleCollapseStage.Shaken;
            }

            return requiredStage > currentStage ? requiredStage : currentStage;
        }

        private static IReadOnlyList<MoraleStageTransition> BuildTransitions(
            MoraleCollapseStage from,
            MoraleCollapseStage to)
        {
            if (to <= from)
            {
                return Array.Empty<MoraleStageTransition>();
            }

            List<MoraleStageTransition> transitions = new List<MoraleStageTransition>((int)to - (int)from);
            MoraleCollapseStage current = from;
            while (current < to)
            {
                MoraleCollapseStage next = (MoraleCollapseStage)((int)current + 1);
                transitions.Add(new MoraleStageTransition(current, next));
                current = next;
            }

            return transitions.AsReadOnly();
        }

        private static int AddClamped(int current, int delta, int minimum, int maximum)
        {
            long value = (long)current + delta;
            return (int)Math.Max(minimum, Math.Min(maximum, value));
        }
    }
}
