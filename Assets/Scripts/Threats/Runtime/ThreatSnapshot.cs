using ZombieCardSurvive.Threats.Data;

namespace ZombieCardSurvive.Threats.Runtime
{
    public sealed class ThreatSnapshot
    {
        public ThreatSnapshot(
            ThreatInputSnapshot input,
            int timeThreat,
            int developmentThreat,
            int worldThreat,
            int worldThreatTier,
            ThreatBand worldThreatBand,
            float baseAttention,
            ThreatBand baseAttentionBand)
        {
            Input = input;
            TimeThreat = timeThreat;
            DevelopmentThreat = developmentThreat;
            WorldThreat = worldThreat;
            WorldThreatTier = worldThreatTier;
            WorldThreatBand = worldThreatBand;
            BaseAttention = baseAttention;
            BaseAttentionBand = baseAttentionBand;
        }

        public ThreatInputSnapshot Input { get; }
        public int TimeThreat { get; }
        public int DevelopmentThreat { get; }
        public int WorldThreat { get; }
        public int WorldThreatTier { get; }
        public ThreatBand WorldThreatBand { get; }
        public float BaseAttention { get; }
        public ThreatBand BaseAttentionBand { get; }
    }
}
