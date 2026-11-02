using System;
using System.Collections.Generic;
using ZombieCardSurvive.Systems;
using ZombieCardSurvive.Systems.Transactions;

namespace ZombieCardSurvive.Run
{
    public enum SettlementStep { Resources, Morale, Stage, Complete }

    /// <summary>Only presentation progression; no payments, randomness or gameplay phase changes.</summary>
    public sealed class SettlementPresentationCoordinator
    {
        private int generation;
        private bool completed;
        private Action finished;
        private readonly List<SettlementStep> steps = new List<SettlementStep>();
        private int index;
        public SettlementStep Current => index < steps.Count ? steps[index] : SettlementStep.Complete;
        public int Generation => generation;
        public void Begin(ValueTransactionResult result, Action onFinished)
        {
            generation++; completed = false; index = 0; finished = onFinished; steps.Clear();
            steps.Add(SettlementStep.Resources); steps.Add(SettlementStep.Morale);
            if (result.Morale != null && result.Morale.ChangeResult.StageChanged) steps.Add(SettlementStep.Stage);
        }
        public bool Advance(int expectedGeneration)
        {
            if (completed || expectedGeneration != generation) return false;
            index++;
            if (Current == SettlementStep.Complete)
            {
                completed = true; var callback = finished; finished = null; callback?.Invoke();
            }
            return true;
        }
        public void Cancel() { generation++; completed = true; finished = null; steps.Clear(); index = 0; }
    }

    public static class SettlementResultFormatter
    {
        public static string StageSummary(ValueTransactionResult result)
        {
            var change = result.Morale?.ChangeResult;
            if (change == null || !change.StageChanged) return string.Empty;
            var oldStage = result.Profile?.GetStage(change.StageBefore);
            var newStage = result.Profile?.GetStage(change.StageAfter);
            return $"Stage {(int)change.StageBefore}｜{Describe(oldStage)}｜已移除\n"
                + $"Stage {(int)change.StageAfter}｜{Describe(newStage)}｜已添加\n本局不可恢復";
        }
        private static string Describe(MoraleStageSnapshot stage)
        {
            if (stage == null) return "未提供效果資料";
            var parts = new List<string>();
            if (stage.ResourceLossPerRound != 0) parts.Add($"每輪失去 {stage.ResourceLossPerRound} 資源");
            if (stage.MaximumEnergyModifier != 0) parts.Add($"最大能量 {stage.MaximumEnergyModifier:+#;-#;0}");
            return parts.Count == 0 ? "無效果" : string.Join("、", parts);
        }
        public static string CollapseSummary(ValueTransactionResult result)
        {
            var c = result.Morale?.ChangeResult;
            if (c == null) return "基地已停止運作，完整原因尚未提供。";
            var lines = new List<string> {
                "居民已無法繼續合作，基地被迫解散或放棄據點。",
                $"最後交易：{result.Source}",
                $"主要負向來源：{SourceName(result.PrimaryMoraleLossSource)}（依本次要求損失排序）",
                $"士氣：{c.MoraleBefore} → {c.MoraleAfter}（實際 {c.AppliedMoraleDelta:+#;-#;0}）",
                $"士氣上限：{c.MaximumMoraleBefore} → {c.MaximumMoraleAfter}", "本次來源（要求變化，合併後受上限限制）：" };
            foreach (var entry in result.Morale.Contributions)
                lines.Add($"{SourceName(entry.Source)}：士氣 {entry.MoraleDelta:+#;-#;0}，上限 {entry.MaximumMoraleDelta:+#;-#;0}");
            return string.Join("\n", lines);
        }
        private static string SourceName(string source)
        {
            if (source == "FoodShortage") return "食物短缺";
            if (source == "EncouragingMeal") return "鼓舞餐點";
            if (source == "HighMoraleDrift") return "高士氣自然回落";
            if (source.StartsWith("Zombie")) return "殭屍漏防（" + source + "）";
            return source;
        }
    }
}
