using System;

namespace ZombieCardSurvive.Buildings.Runtime
{
    [Serializable]
    public class BuildingResolutionReport
    {
        public int CancelledConstructions { get; internal set; }
        public int CompletedConstructions { get; internal set; }
        public int CancelledCraftJobs { get; internal set; }
        public int CompletedCraftJobs { get; internal set; }
        public int RemovedConstructionCards { get; internal set; }
        public int ProducedCards { get; internal set; }
    }
}
