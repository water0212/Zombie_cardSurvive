using System;
using System.Collections.Generic;

namespace ZombieCardSurvive.Systems
{
    /// <summary>一次不可逆士氣階段轉換的紀錄。</summary>
    public readonly struct MoraleStageTransition
    {
        public MoraleStageTransition(MoraleCollapseStage from, MoraleCollapseStage to)
        {
            From = from;
            To = to;
        }

        public MoraleCollapseStage From { get; }
        public MoraleCollapseStage To { get; }
    }

    /// <summary>
    /// 一次完整士氣提交的結果。輪末報告與 UI 可共用此物件，不必重新推算門檻。
    /// </summary>
    public sealed class MoraleChangeResult
    {
        private static readonly IReadOnlyList<MoraleStageTransition> NoTransitions
            = Array.Empty<MoraleStageTransition>();

        public MoraleChangeResult(
            string source,
            int moraleBefore,
            int moraleAfter,
            int maximumMoraleBefore,
            int maximumMoraleAfter,
            int requestedMoraleDelta,
            int requestedMaximumMoraleDelta,
            MoraleCollapseStage stageBefore,
            MoraleCollapseStage stageAfter,
            IReadOnlyList<MoraleStageTransition> stageTransitions,
            bool wasCollapsed,
            bool isCollapsed,
            bool didCollapse)
        {
            Source = string.IsNullOrWhiteSpace(source) ? "Unspecified" : source;
            MoraleBefore = moraleBefore;
            MoraleAfter = moraleAfter;
            MaximumMoraleBefore = maximumMoraleBefore;
            MaximumMoraleAfter = maximumMoraleAfter;
            RequestedMoraleDelta = requestedMoraleDelta;
            RequestedMaximumMoraleDelta = requestedMaximumMoraleDelta;
            StageBefore = stageBefore;
            StageAfter = stageAfter;
            StageTransitions = stageTransitions ?? NoTransitions;
            WasCollapsed = wasCollapsed;
            IsCollapsed = isCollapsed;
            DidCollapse = didCollapse;
        }

        public string Source { get; }
        public int MoraleBefore { get; }
        public int MoraleAfter { get; }
        public int MaximumMoraleBefore { get; }
        public int MaximumMoraleAfter { get; }
        public int RequestedMoraleDelta { get; }
        public int RequestedMaximumMoraleDelta { get; }
        public int AppliedMoraleDelta => MoraleAfter - MoraleBefore;
        public int AppliedMaximumMoraleDelta => MaximumMoraleAfter - MaximumMoraleBefore;
        public MoraleCollapseStage StageBefore { get; }
        public MoraleCollapseStage StageAfter { get; }
        public IReadOnlyList<MoraleStageTransition> StageTransitions { get; }
        public bool StageChanged => StageBefore != StageAfter;
        public bool WasCollapsed { get; }
        public bool IsCollapsed { get; }
        public bool DidCollapse { get; }
    }
}
