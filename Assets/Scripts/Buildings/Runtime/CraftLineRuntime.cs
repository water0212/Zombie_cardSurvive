using System;
using System.Collections.Generic;
using ZombieCardSurvive.Buildings.Data;

namespace ZombieCardSurvive.Buildings.Runtime
{
    [Serializable]
    public class CraftLineRuntime
    {
        private readonly List<CraftJobRuntime> jobs = new List<CraftJobRuntime>();

        public CraftLineRuntime(CardCraftInteractionData data)
        {
            Data = data;
        }

        public CardCraftInteractionData Data { get; }
        public IReadOnlyList<CraftJobRuntime> Jobs => jobs;
        public bool HasCapacity => Data != null && jobs.Count < Data.QueueCapacity;

        public bool AddJob(CraftJobRuntime job)
        {
            if (job == null || !HasCapacity)
            {
                return false;
            }

            jobs.Add(job);
            return true;
        }

        public CraftJobRuntime FindJob(string jobId)
        {
            return jobs.Find(job => job != null && job.JobId == jobId);
        }

        public void RemoveJob(CraftJobRuntime job)
        {
            jobs.Remove(job);
        }
    }
}
