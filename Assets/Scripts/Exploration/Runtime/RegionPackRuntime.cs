using System;
using ZombieCardSurvive.Exploration.Data;

namespace ZombieCardSurvive.Exploration.Runtime
{
    [Serializable]
    public sealed class RegionPackRuntime
    {
        internal RegionPackRuntime(CardPackDropEntry source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            Data = source.CardPack;
            PackId = Data != null ? Data.PackId?.Trim() : string.Empty;
            SupplyRarity = source.SupplyRarity;
            InitialCount = source.InitialCount;
            RemainingCount = InitialCount;
            BaseWeight = source.BaseWeight;
        }

        public CardPackData Data { get; }
        public string PackId { get; }
        public RegionSupplyRarity SupplyRarity { get; }
        public int InitialCount { get; }
        public int RemainingCount { get; private set; }
        public int BaseWeight { get; }
        public float CurrentWeight => InitialCount <= 0 || RemainingCount <= 0
            ? 0f
            : BaseWeight * (RemainingCount / (float)InitialCount);
        public bool IsDrawable => CurrentWeight > 0f;

        internal bool TryConsume(int amount)
        {
            if (amount <= 0 || RemainingCount < amount)
            {
                return false;
            }

            RemainingCount -= amount;
            return true;
        }

        internal bool TryAdd(int amount)
        {
            if (amount <= 0 || InitialCount <= 0 || RemainingCount > int.MaxValue - amount)
            {
                return false;
            }

            RemainingCount += amount;
            return true;
        }
    }
}
