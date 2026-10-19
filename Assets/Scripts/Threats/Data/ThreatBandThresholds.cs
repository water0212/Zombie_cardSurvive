using System;
using UnityEngine;

namespace ZombieCardSurvive.Threats.Data
{
    [Serializable]
    public struct ThreatBandThresholds
    {
        [SerializeField] private float mediumMinimum;
        [SerializeField] private float highMinimum;
        [SerializeField] private float extremeMinimum;

        public ThreatBandThresholds(float mediumMinimum, float highMinimum, float extremeMinimum)
        {
            this.mediumMinimum = mediumMinimum;
            this.highMinimum = highMinimum;
            this.extremeMinimum = extremeMinimum;
        }

        public static ThreatBandThresholds DefaultScore => new ThreatBandThresholds(2f, 5f, 8f);
        public static ThreatBandThresholds DefaultRegionPressure => new ThreatBandThresholds(0.20f, 0.35f, 0.60f);

        public float MediumMinimum => mediumMinimum;
        public float HighMinimum => highMinimum;
        public float ExtremeMinimum => extremeMinimum;

        public ThreatBandThresholds Sanitized()
        {
            float medium = Mathf.Max(0f, mediumMinimum);
            float high = Mathf.Max(medium, highMinimum);
            float extreme = Mathf.Max(high, extremeMinimum);
            return new ThreatBandThresholds(medium, high, extreme);
        }

        public ThreatBandThresholds Sanitized(ThreatBandThresholds fallback)
        {
            if (mediumMinimum <= 0f && highMinimum <= 0f && extremeMinimum <= 0f)
            {
                return fallback.Sanitized();
            }

            return Sanitized();
        }

        public ThreatBand Classify(float value)
        {
            ThreatBandThresholds thresholds = Sanitized();
            float safeValue = Mathf.Max(0f, value);
            if (safeValue >= thresholds.ExtremeMinimum)
            {
                return ThreatBand.Extreme;
            }

            if (safeValue >= thresholds.HighMinimum)
            {
                return ThreatBand.High;
            }

            return safeValue >= thresholds.MediumMinimum
                ? ThreatBand.Medium
                : ThreatBand.Low;
        }
    }
}
