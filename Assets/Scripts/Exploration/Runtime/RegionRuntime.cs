using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Exploration.Data;

namespace ZombieCardSurvive.Exploration.Runtime
{
    [Serializable]
    public sealed class RegionRuntime
    {
        public const float LowDangerUpperBound = 0.20f;
        public const float MediumDangerUpperBound = 0.35f;
        public const float HighDangerUpperBound = 0.60f;

        private readonly Dictionary<string, RegionPackRuntime> packStocks =
            new Dictionary<string, RegionPackRuntime>(StringComparer.Ordinal);

        public RegionRuntime(ExplorationRegionData data)
        {
            Data = data != null ? data : throw new ArgumentNullException(nameof(data));
            RegionId = data.RegionId?.Trim() ?? string.Empty;
            InitialZombieCount = data.InitialZombieCount;
            RemainingZombieCount = InitialZombieCount;
            Heat = 0f;

            BuildPackStocks(data.CardPackDrops);
        }

        public event Action<RegionRuntime> Changed;

        public ExplorationRegionData Data { get; }
        public string RegionId { get; }
        public IReadOnlyDictionary<string, RegionPackRuntime> PackStocks => packStocks;
        public int InitialZombieCount { get; }
        public int RemainingZombieCount { get; private set; }
        public float Heat { get; private set; }
        public bool WasExploredSinceLastDailyDecay { get; private set; }

        public int InitialPackCount
        {
            get
            {
                int total = 0;
                foreach (RegionPackRuntime stock in packStocks.Values)
                {
                    total += stock.InitialCount;
                }

                return total;
            }
        }

        public int RemainingPackCount
        {
            get
            {
                int total = 0;
                foreach (RegionPackRuntime stock in packStocks.Values)
                {
                    total += stock.RemainingCount;
                }

                return total;
            }
        }

        public float SearchProgress => InitialPackCount <= 0
            ? 1f
            : Mathf.Clamp01(1f - RemainingPackCount / (float)InitialPackCount);

        public float RemainingPackRatio => InitialPackCount <= 0
            ? 0f
            : Mathf.Clamp01(RemainingPackCount / (float)InitialPackCount);

        public float Clearance => InitialZombieCount <= 0
            ? (RemainingZombieCount > 0 ? 0f : 1f)
            : Mathf.Clamp01(1f - RemainingZombieCount / (float)InitialZombieCount);

        public float HeatModifier => Heat;
        public float ClearanceModifier => Data.BaseEncounterChance * Clearance;
        public float EncounterChance => RemainingZombieCount <= 0
            ? 0f
            : Mathf.Clamp01(Data.BaseEncounterChance + HeatModifier - ClearanceModifier);

        public DangerLevel RelativeDangerLevel => ClassifyDanger(EncounterChance);

        public bool HasRemainingPacks => RemainingPackCount > 0;
        public bool HasDrawablePacks => GetTotalCurrentWeight() > 0f;

        public RegionResourceState ResourceState
        {
            get
            {
                float remainingRatio = RemainingPackRatio;
                if (remainingRatio <= 0f)
                {
                    return RegionResourceState.Depleted;
                }

                if (remainingRatio <= 1f / 3f)
                {
                    return RegionResourceState.Scarce;
                }

                return remainingRatio <= 2f / 3f
                    ? RegionResourceState.Normal
                    : RegionResourceState.Abundant;
            }
        }

        public static DangerLevel ClassifyDanger(float encounterChance)
        {
            float chance = Mathf.Clamp01(encounterChance);
            if (chance <= LowDangerUpperBound)
            {
                return DangerLevel.Low;
            }

            if (chance <= MediumDangerUpperBound)
            {
                return DangerLevel.Medium;
            }

            return chance <= HighDangerUpperBound
                ? DangerLevel.High
                : DangerLevel.Extreme;
        }

        public bool TryGetPack(string packId, out RegionPackRuntime stock)
        {
            stock = null;
            if (string.IsNullOrWhiteSpace(packId))
            {
                return false;
            }

            return packStocks.TryGetValue(packId.Trim(), out stock);
        }

        public bool TryConsumePack(string packId, int amount = 1)
        {
            if (!TryGetPack(packId, out RegionPackRuntime stock) || !stock.TryConsume(amount))
            {
                return false;
            }

            NotifyChanged();
            return true;
        }

        public float GetCurrentWeight(string packId)
        {
            return TryGetPack(packId, out RegionPackRuntime stock) ? stock.CurrentWeight : 0f;
        }

        public float GetPackProbability(string packId)
        {
            if (!TryGetPack(packId, out RegionPackRuntime stock))
            {
                return 0f;
            }

            float totalWeight = GetTotalCurrentWeight();
            return totalWeight > 0f ? stock.CurrentWeight / totalWeight : 0f;
        }

        public float GetTotalCurrentWeight()
        {
            float totalWeight = 0f;
            foreach (RegionPackRuntime stock in packStocks.Values)
            {
                totalWeight += stock.CurrentWeight;
            }

            return totalWeight;
        }

        public bool TryDrawPack(out RegionPackRuntime selectedPack)
        {
            return TryDrawPack(UnityEngine.Random.value, out selectedPack);
        }

        public bool TryRestockPack(string packId, int amount)
        {
            if (!TryGetPack(packId, out RegionPackRuntime stock) || !stock.TryAdd(amount))
            {
                return false;
            }

            NotifyChanged();
            return true;
        }

        public bool TryConsumeZombie(int amount = 1)
        {
            if (amount <= 0 || RemainingZombieCount < amount)
            {
                return false;
            }

            RemainingZombieCount -= amount;
            NotifyChanged();
            return true;
        }

        public bool TryRestockZombie(int amount)
        {
            if (amount <= 0 || RemainingZombieCount > int.MaxValue - amount)
            {
                return false;
            }

            RemainingZombieCount += amount;
            NotifyChanged();
            return true;
        }

        public void RecordExploration()
        {
            WasExploredSinceLastDailyDecay = true;
            AddHeat(Data.HeatPerExploration);
        }

        public bool ResolveDailyHeatDecay()
        {
            if (WasExploredSinceLastDailyDecay)
            {
                WasExploredSinceLastDailyDecay = false;
                return false;
            }

            return DecayHeat(Data.DailyHeatDecay);
        }

        public bool AddHeat(float amount)
        {
            if (amount <= 0f)
            {
                return false;
            }

            Heat += amount;
            NotifyChanged();
            return true;
        }

        public bool DecayHeat(float amount)
        {
            if (amount <= 0f || Heat <= 0f)
            {
                return false;
            }

            Heat = Mathf.Max(0f, Heat - amount);
            NotifyChanged();
            return true;
        }

        private bool TryDrawPack(float normalizedRoll, out RegionPackRuntime selectedPack)
        {
            selectedPack = null;
            float totalWeight = GetTotalCurrentWeight();
            if (totalWeight <= 0f)
            {
                return false;
            }

            float roll = Mathf.Clamp01(normalizedRoll) * totalWeight;
            float cumulative = 0f;
            RegionPackRuntime lastDrawablePack = null;
            foreach (RegionPackRuntime stock in packStocks.Values)
            {
                if (!stock.IsDrawable)
                {
                    continue;
                }

                lastDrawablePack = stock;
                cumulative += stock.CurrentWeight;
                if (roll < cumulative)
                {
                    selectedPack = stock;
                    break;
                }
            }

            selectedPack ??= lastDrawablePack;
            if (selectedPack == null || !selectedPack.TryConsume(1))
            {
                selectedPack = null;
                return false;
            }

            NotifyChanged();
            return true;
        }

        private void BuildPackStocks(IReadOnlyList<CardPackDropEntry> entries)
        {
            if (entries == null)
            {
                return;
            }

            for (int index = 0; index < entries.Count; index++)
            {
                CardPackDropEntry entry = entries[index];
                if (entry?.CardPack == null)
                {
                    Debug.LogWarning(
                        $"Region '{GetRegionLabel()}' skipped card pack entry {index} because CardPackData is missing.",
                        Data);
                    continue;
                }

                string packId = entry.CardPack.PackId?.Trim();
                if (string.IsNullOrWhiteSpace(packId))
                {
                    Debug.LogWarning(
                        $"Region '{GetRegionLabel()}' skipped card pack '{entry.CardPack.name}' because PackId is missing.",
                        Data);
                    continue;
                }

                if (packStocks.ContainsKey(packId))
                {
                    Debug.LogWarning(
                        $"Region '{GetRegionLabel()}' skipped duplicate PackId '{packId}'. The first entry was kept.",
                        Data);
                    continue;
                }

                packStocks.Add(packId, new RegionPackRuntime(entry));
            }
        }

        private string GetRegionLabel()
        {
            if (!string.IsNullOrWhiteSpace(Data.DisplayName))
            {
                return Data.DisplayName;
            }

            return !string.IsNullOrWhiteSpace(RegionId) ? RegionId : Data.name;
        }

        private void NotifyChanged()
        {
            Changed?.Invoke(this);
        }
    }
}
