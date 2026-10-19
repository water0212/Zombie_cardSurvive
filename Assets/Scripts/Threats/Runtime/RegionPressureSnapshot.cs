using ZombieCardSurvive.Threats.Data;

namespace ZombieCardSurvive.Threats.Runtime
{
    public sealed class RegionPressureSnapshot
    {
        public RegionPressureSnapshot(
            string regionId,
            float basePressure,
            float heatContribution,
            float clearanceReduction,
            float pressure,
            ThreatBand pressureBand)
        {
            RegionId = regionId;
            BasePressure = basePressure;
            HeatContribution = heatContribution;
            ClearanceReduction = clearanceReduction;
            Pressure = pressure;
            PressureBand = pressureBand;
        }

        public string RegionId { get; }
        public float BasePressure { get; }
        public float HeatContribution { get; }
        public float ClearanceReduction { get; }
        public float Pressure { get; }
        public ThreatBand PressureBand { get; }
    }
}
