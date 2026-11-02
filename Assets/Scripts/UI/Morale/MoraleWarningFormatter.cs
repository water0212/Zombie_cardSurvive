using System;
using System.Collections.Generic;
using ZombieCardSurvive.Run;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.UI.Morale
{
    public static class MoraleWarningFormatter
    {
        public static string StageName(MoraleCollapseStage stage)
        {
            switch (stage)
            {
                case MoraleCollapseStage.Shaken: return "動搖";
                case MoraleCollapseStage.Brink: return "瀕臨崩潰";
                default: return "安定";
            }
        }

        public static string PyramidMark(MoraleCollapseStage stage)
        {
            switch (stage)
            {
                case MoraleCollapseStage.Shaken: return "△ 裂痕";
                case MoraleCollapseStage.Brink: return "▽ 破碎";
                default: return "▲ 完整";
            }
        }

        public static string BuildMoraleWarning(RoundEndPreview preview)
        {
            if (preview == null) return string.Empty;
            string permanent = preview.HasPermanentDamage ? "\n另有殭屍將造成永久創傷；請查看殭屍詳情" : string.Empty;
            if (preview.WillCollapse)
                return "基地恐將撐不下去\n依目前狀態，本輪結算後士氣將歸零" + permanent;
            if (preview.WillCrossStage)
                return $"士氣恐將跌入「{StageName(preview.StageAfter)}」\n跨過後，本局無法恢復此階段" + permanent;
            if (preview.HasPermanentDamage)
                return "有殭屍將造成永久創傷\n查看殭屍了解詳情";
            return $"依目前已知狀態：士氣 {preview.MoraleBefore} → {preview.MoraleAfter}";
        }

        public static string BuildFoodWarning(RoundEndPreview preview)
        {
            if (preview == null) return string.Empty;
            if (preview.FoodShortage > 0)
                return $"糧食不夠｜本輪預計短缺 {preview.FoodShortage}";
            if (preview.EncouragingMealAttempted && !preview.EncouragingMealSucceeded)
                return "這輪無法加餐｜" + MealFailureText(preview.EncouragingMealFailureReason);
            return $"本輪預計消耗 {preview.FoodDemand}｜結算後 {preview.FoodAfter}";
        }

        public static string BuildResourceWarning(RoundEndPreview preview)
        {
            if (preview == null || preview.ResourceLoss <= 0) return string.Empty;
            return $"動搖影響｜本輪資源 -{preview.ResourceLoss}（剩餘 {preview.ResourceAfter}）";
        }

        public static string BuildCombatWarning(RoundEndPreview preview)
        {
            if (preview == null || preview.CombatOvercapLoss <= 0) return string.Empty;
            return $"超量戰力衰減 -{preview.CombatOvercapLoss}｜結算後 {preview.CombatAfter}";
        }

        public static string BuildDetails(RoundEndPreview preview)
        {
            if (preview == null) return "士氣資訊尚未就緒";
            string profile = MoralePyramidSystem.IsInitialized
                ? MoralePyramidSystem.Profile.DisplayName : "尚未選定";
            string next = preview.StageBefore == MoraleCollapseStage.Stable
                ? $"下一門檻：低於 {MoraleSystem.StableThreshold}"
                : preview.StageBefore == MoraleCollapseStage.Shaken
                    ? $"下一門檻：低於 {MoraleSystem.CriticalThreshold}"
                    : "已無下一降階；士氣歸零時基地崩潰";
            string effect = MoralePyramidSystem.IsInitialized
                ? MoralePyramidSystem.ActiveStage.Description : "效果尚未初始化";
            return $"目前士氣：{preview.MoraleBefore}\n目前上限：{preview.MaximumMoraleBefore}\n"
                + $"本局永久失去：{MoraleSystem.AbsoluteMaximumMorale - preview.MaximumMoraleBefore}\n"
                + $"目前階段：{StageName(preview.StageBefore)}｜{PyramidMark(preview.StageBefore)}\n"
                + $"{next}\n方案：{profile}\n目前效果：{effect}\n\n回復士氣不會恢復已失去的階段。";
        }

        public static string BuildConfirmation(RoundEndPreview preview)
        {
            if (preview == null) return string.Empty;
            List<string> lines = new List<string>();
            if (preview.WillCollapse) lines.Add("• 基地可能在本輪結算後崩潰");
            if (preview.WillCrossStage) lines.Add($"• 士氣將永久跌入「{StageName(preview.StageAfter)}」");
            if (preview.HasPermanentDamage) lines.Add("• 場上殭屍將造成永久創傷；請查看各殭屍詳情");
            lines.Add("你可以返回基地，查看威脅並調整安排。");
            return string.Join("\n", lines);
        }

        private static string MealFailureText(ZombieCardSurvive.Systems.EncouragingMealFailureReason reason)
        {
            switch (reason)
            {
                case EncouragingMealFailureReason.FoodShortage: return "固定供應已有短缺";
                case EncouragingMealFailureReason.MoraleAtMaximum: return "士氣已達目前上限";
                case EncouragingMealFailureReason.NoExtraCost: return "目前沒有可支付的加餐成本";
                case EncouragingMealFailureReason.InsufficientPermanentFood: return "可用永久食物不足";
                default: return "目前條件不符";
            }
        }
    }
}
