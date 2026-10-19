using System;
using System.Collections.Generic;
using System.Globalization;

namespace ZombieCardSurvive.Events.Runtime
{
    public static class EventTextFormatter
    {
        public static string Resolve(
            string template,
            IReadOnlyList<ResolvedGameEffectRuntime> effects)
        {
            if (string.IsNullOrEmpty(template) || effects == null)
            {
                return template ?? string.Empty;
            }

            string resolved = template;
            for (int index = 0; index < effects.Count; index++)
            {
                ResolvedGameEffectRuntime effect = effects[index];
                if (effect == null)
                {
                    continue;
                }

                string value = effect.FinalAmount.ToString(CultureInfo.InvariantCulture);
                string magnitude = Math.Abs((long)effect.FinalAmount).ToString(CultureInfo.InvariantCulture);
                resolved = resolved.Replace($"{{value:{index}}}", value);
                resolved = resolved.Replace($"{{magnitude:{index}}}", magnitude);
            }

            return resolved;
        }

        public static string ResolveExecution(
            string template,
            IReadOnlyList<EventForcedLossResult> forcedLosses)
        {
            if (string.IsNullOrEmpty(template) || forcedLosses == null)
            {
                return template ?? string.Empty;
            }

            string resolved = template;
            for (int index = 0; index < forcedLosses.Count; index++)
            {
                EventForcedLossResult loss = forcedLosses[index];
                string consumed = loss.Consumed.ToString(CultureInfo.InvariantCulture);
                string shortage = loss.Shortage.ToString(CultureInfo.InvariantCulture);
                resolved = resolved.Replace($"{{paid:{loss.SourceEffectIndex}}}", consumed);
                resolved = resolved.Replace($"{{shortage:{loss.SourceEffectIndex}}}", shortage);
            }

            return resolved;
        }
    }
}
