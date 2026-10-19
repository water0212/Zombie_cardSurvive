using System;
using ZombieCardSurvive.Events.Data;

namespace ZombieCardSurvive.Events.Runtime
{
    public static class EventEffectScalingResolver
    {
        public static int ResolveScaleTier(
            EventEffectScalingData scaling,
            EventMagnitudeSnapshot snapshot)
        {
            return scaling != null && snapshot != null
                ? snapshot.GetScaleTier(scaling.ScaleSource)
                : 1;
        }

        public static float ResolveMultiplier(
            EventEffectScalingData scaling,
            EventMagnitudeSnapshot snapshot)
        {
            int scaleTier = ResolveScaleTier(scaling, snapshot);
            float strength = scaling != null ? scaling.ScalingStrength : 0f;
            return 1f + (scaleTier - 1) * strength;
        }

        public static int ResolveAmount(
            int baseAmount,
            EventEffectScalingData scaling,
            EventMagnitudeSnapshot snapshot)
        {
            if (baseAmount == 0 || scaling == null)
            {
                return baseAmount;
            }

            int sign = baseAmount < 0 ? -1 : 1;
            double magnitude = Math.Abs((long)baseAmount)
                * ResolveMultiplier(scaling, snapshot);
            magnitude = Math.Min(int.MaxValue, magnitude);

            long roundedMagnitude;
            switch (scaling.RoundingMode)
            {
                case EventScaleRoundingMode.Floor:
                    roundedMagnitude = (long)Math.Floor(magnitude);
                    break;
                case EventScaleRoundingMode.Ceil:
                    roundedMagnitude = (long)Math.Ceiling(magnitude);
                    break;
                default:
                    roundedMagnitude = (long)Math.Round(magnitude, MidpointRounding.AwayFromZero);
                    break;
            }

            roundedMagnitude = Math.Max(scaling.MinimumFinalMagnitude, roundedMagnitude);
            if (scaling.MaximumFinalMagnitude > 0)
            {
                roundedMagnitude = Math.Min(scaling.MaximumFinalMagnitude, roundedMagnitude);
            }

            roundedMagnitude = Math.Min(int.MaxValue, roundedMagnitude);
            return sign < 0 ? -(int)roundedMagnitude : (int)roundedMagnitude;
        }
    }
}
