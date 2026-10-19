using System;
using System.Collections.Generic;

namespace ZombieCardSurvive.Systems
{
    /// <summary>完整士氣交易中的一項可追溯來源。</summary>
    public readonly struct MoraleBatchContribution
    {
        public MoraleBatchContribution(
            string source,
            int moraleDelta,
            int maximumMoraleDelta = 0)
        {
            Source = string.IsNullOrWhiteSpace(source) ? "Unspecified" : source;
            MoraleDelta = moraleDelta;
            MaximumMoraleDelta = maximumMoraleDelta;
        }

        public string Source { get; }
        public int MoraleDelta { get; }
        public int MaximumMoraleDelta { get; }
    }

    /// <summary>建立一次完整士氣交易；加入順序只影響報告，不影響最終淨值。</summary>
    public sealed class MoraleBatchRequest
    {
        private readonly List<MoraleBatchContribution> contributions =
            new List<MoraleBatchContribution>();

        public MoraleBatchRequest(string source)
        {
            Source = string.IsNullOrWhiteSpace(source) ? "UnspecifiedBatch" : source;
        }

        public string Source { get; }
        public IReadOnlyList<MoraleBatchContribution> Contributions => contributions;
        public int Count => contributions.Count;
        public int NetMoraleDelta { get; private set; }
        public int NetMaximumMoraleDelta { get; private set; }

        public void Add(string source, int moraleDelta, int maximumMoraleDelta = 0)
        {
            if (moraleDelta == 0 && maximumMoraleDelta == 0)
            {
                return;
            }

            contributions.Add(new MoraleBatchContribution(
                source,
                moraleDelta,
                maximumMoraleDelta));
            NetMoraleDelta = AddClamped(NetMoraleDelta, moraleDelta);
            NetMaximumMoraleDelta = AddClamped(NetMaximumMoraleDelta, maximumMoraleDelta);
        }

        private static int AddClamped(int left, int right)
        {
            long value = (long)left + right;
            return (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, value));
        }
    }

    /// <summary>士氣批次明細與 014A 單次狀態提交結果。</summary>
    public sealed class MoraleBatchResult
    {
        public MoraleBatchResult(
            string source,
            IReadOnlyList<MoraleBatchContribution> contributions,
            int netMoraleDelta,
            int netMaximumMoraleDelta,
            MoraleChangeResult changeResult)
        {
            Source = string.IsNullOrWhiteSpace(source) ? "UnspecifiedBatch" : source;
            Contributions = contributions ?? Array.Empty<MoraleBatchContribution>();
            NetMoraleDelta = netMoraleDelta;
            NetMaximumMoraleDelta = netMaximumMoraleDelta;
            ChangeResult = changeResult;
        }

        public string Source { get; }
        public IReadOnlyList<MoraleBatchContribution> Contributions { get; }
        public int NetMoraleDelta { get; }
        public int NetMaximumMoraleDelta { get; }
        public MoraleChangeResult ChangeResult { get; }
    }
}
