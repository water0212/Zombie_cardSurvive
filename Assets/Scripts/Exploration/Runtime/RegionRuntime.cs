using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Exploration.Data;

namespace ZombieCardSurvive.Exploration.Runtime
{
    [Serializable]
    public sealed class RegionRuntime
    {
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

        public float Clearance => InitialZombieCount <= 0
            ? 1f
            : Mathf.Clamp01(1f - RemainingZombieCount / (float)InitialZombieCount);

        public bool HasRemainingPacks => RemainingPackCount > 0;

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
