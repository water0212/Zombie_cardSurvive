using System.Collections.Generic;
using ZombieCardSurvive.Effects.Data;
using ZombieCardSurvive.Events.Runtime;

namespace ZombieCardSurvive.Events.UI
{
    /// <summary>只把事件執行資料轉成玩家可讀文字，不讀取或改動遊戲狀態。</summary>
    public static class EventPresentationFormatter
    {
        public static string BuildChoiceCost(EventChoiceAvailability availability)
        {
            if (availability == null)
            {
                return string.Empty;
            }

            var parts = new List<string>();
            if (availability.RequiredFood > 0)
            {
                parts.Add($"食物 -{availability.RequiredFood}");
            }

            if (availability.RequiredResource > 0)
            {
                parts.Add($"資源 -{availability.RequiredResource}");
            }

            return parts.Count > 0 ? string.Join("　", parts) : "無代價";
        }

        public static string BuildResultSummary(
            ResolvedEventChoiceRuntime choice,
            EventChoiceExecutionResult result,
            bool coreValuesShownAsIcons = false)
        {
            if (result == null || !result.Succeeded)
            {
                return string.Empty;
            }

            var lines = new List<string>();
            if (result.ValueChanges == null)
            {
                AddPayment(lines, result.Availability);
                AddForcedLosses(lines, result.ForcedLosses);
            }
            else if (!coreValuesShownAsIcons)
            {
                AddSigned(lines, "食物", result.ValueChanges.Food);
                AddSigned(lines, "資源", result.ValueChanges.Resource);
                AddSigned(lines, "戰鬥值", result.ValueChanges.Combat);
                AddSigned(lines, "能量", result.ValueChanges.Energy);
            }

            if (result.MoraleSettlement?.ChangeResult != null)
            {
                AddSigned(lines, "士氣", result.MoraleSettlement.ChangeResult.AppliedMoraleDelta);
                AddSigned(lines, "士氣上限", result.MoraleSettlement.ChangeResult.AppliedMaximumMoraleDelta);
            }

            AddAppliedEffects(lines, result.AppliedEffects, result.ValueChanges != null);
            return lines.Count > 0 ? string.Join("　｜　", lines) : coreValuesShownAsIcons ? string.Empty : "沒有數值變化";
        }

        private static void AddPayment(List<string> lines, EventChoiceAvailability availability)
        {
            if (availability == null)
            {
                return;
            }

            if (availability.RequiredFood > 0)
            {
                lines.Add($"食物 -{availability.RequiredFood}");
            }

            if (availability.RequiredResource > 0)
            {
                lines.Add($"資源 -{availability.RequiredResource}");
            }
        }

        private static void AddForcedLosses(
            List<string> lines,
            IReadOnlyList<EventForcedLossResult> forcedLosses)
        {
            if (forcedLosses == null)
            {
                return;
            }

            foreach (EventForcedLossResult loss in forcedLosses)
            {
                if (loss.Consumed <= 0)
                {
                    continue;
                }

                string label = loss.ValueEffectType == ValueEffectType.SpendFood ? "食物" : "資源";
                lines.Add($"{label} -{loss.Consumed}");
            }
        }

        private static void AddAppliedEffects(
            List<string> lines,
            IReadOnlyList<ResolvedGameEffectRuntime> appliedEffects,
            bool hasActualValues)
        {
            if (appliedEffects == null)
            {
                return;
            }

            foreach (ResolvedGameEffectRuntime effect in appliedEffects)
            {
                if (effect?.Source == null
                    || effect.Source.IsChoiceCost || effect.Source.IsForcedLoss
                    || effect.IsMoraleEffect)
                {
                    continue;
                }

                if (effect.Source.Category == GameEffectCategory.Value)
                {
                    if (hasActualValues && IsCoreValue(effect.Source.ValueEffectType)) continue;
                    AddValueEffect(lines, effect.Source.ValueEffectType, effect.FinalAmount);
                }
                else if (effect.Source.IsSpawnHorde)
                {
                    lines.Add($"屍群 +{effect.FinalAmount}");
                }
                else if (effect.Source.IsNextRoundEffect)
                {
                    lines.Add("下個時段效果已安排");
                }
            }
        }

        private static void AddValueEffect(List<string> lines, ValueEffectType type, int amount)
        {
            switch (type)
            {
                case ValueEffectType.AddFood: AddSigned(lines, "食物", amount); break;
                case ValueEffectType.SpendFood: AddSigned(lines, "食物", -amount); break;
                case ValueEffectType.AddResource: AddSigned(lines, "資源", amount); break;
                case ValueEffectType.SpendResource: AddSigned(lines, "資源", -amount); break;
                case ValueEffectType.AddCurrentEnergy: AddSigned(lines, "能量", amount); break;
                case ValueEffectType.AddPendingDamage: AddSigned(lines, "戰鬥值", amount); break;
                case ValueEffectType.ClearPendingDamage: lines.Add("戰鬥值已清除"); break;
                case ValueEffectType.IncreaseMaxEnergy: AddSigned(lines, "能量上限", amount); break;
                case ValueEffectType.DecreaseMaxEnergy: AddSigned(lines, "能量上限", -amount); break;
                case ValueEffectType.AddZombieThreat: AddSigned(lines, "殭屍威脅", amount); break;
                case ValueEffectType.RemoveZombieThreat: AddSigned(lines, "殭屍威脅", -amount); break;
                case ValueEffectType.AddTemporaryFood: AddSigned(lines, "臨時食物", amount); break;
                case ValueEffectType.AddRoundFoodConsumptionModifier: AddSigned(lines, "下輪食物需求", amount); break;
                case ValueEffectType.AddFoodProduction: AddSigned(lines, "食物產量", amount); break;
                case ValueEffectType.AddResourceProduction: AddSigned(lines, "資源產量", amount); break;
                case ValueEffectType.RefillEnergy: lines.Add("能量已補滿"); break;
            }
        }

        private static bool IsCoreValue(ValueEffectType type)
        {
            return type == ValueEffectType.AddFood || type == ValueEffectType.SpendFood
                || type == ValueEffectType.AddResource || type == ValueEffectType.SpendResource
                || type == ValueEffectType.AddPendingDamage || type == ValueEffectType.ClearPendingDamage
                || type == ValueEffectType.AddCurrentEnergy || type == ValueEffectType.RefillEnergy;
        }

        private static void AddSigned(List<string> lines, string label, long amount)
        {
            if (amount != 0)
            {
                lines.Add($"{label} {(amount > 0 ? "+" : string.Empty)}{amount}");
            }
        }
    }
}
