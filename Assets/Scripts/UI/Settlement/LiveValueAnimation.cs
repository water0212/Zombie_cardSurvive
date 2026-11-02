using System;
using ZombieCardSurvive.Systems.Transactions;

namespace ZombieCardSurvive.UI.Settlement
{
    /// <summary>One retargetable local animation. It has no input locks, coroutines or gameplay writes.</summary>
    public sealed class LiveValueAnimation
    {
        private static readonly ValueId[] Ids = (ValueId[])Enum.GetValues(typeof(ValueId));
        private readonly double[] from = new double[Ids.Length];
        private readonly double[] to = new double[Ids.Length];
        private readonly long[] deltas = new long[Ids.Length];
        private double elapsed, duration;
        private bool hasFrame, hasMotion;
        public ValueSnapshot Target { get; private set; }
        public bool IsActive => hasFrame && hasMotion && elapsed < duration;
        public float Progress => !IsActive ? 1f : (float)Math.Min(1d, elapsed / duration);
        public int Displayed(ValueId id)
        {
            int index = (int)id;
            double value = IsActive ? from[index] + (to[index] - from[index]) * Progress : to[index];
            return (int)Math.Round(value);
        }
        public long Delta(ValueId id) => deltas[(int)id];
        public void Submit(ValueTransactionResult result, float seconds)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            bool retarget = IsActive;
            double progress = Progress;
            bool motion = false;
            foreach (var id in Ids)
            {
                int index = (int)id;
                double visible = retarget ? from[index] + (to[index] - from[index]) * progress : result.Before.Get(id);
                from[index] = visible;
                to[index] = result.After.Get(id);
                deltas[index] = (retarget ? deltas[index] : 0L) + result.Delta(id);
                motion |= visible != to[index];
            }
            Target = result.After;
            elapsed = 0d; duration = Math.Max(.001d, seconds);
            hasFrame = true; hasMotion = motion;
        }
        public void Tick(float seconds) { if (IsActive) elapsed += Math.Max(0d, seconds); }
        public void Clear()
        {
            hasFrame = hasMotion = false; elapsed = duration = 0d;
            Array.Clear(from, 0, from.Length); Array.Clear(to, 0, to.Length); Array.Clear(deltas, 0, deltas.Length);
        }
    }
}
