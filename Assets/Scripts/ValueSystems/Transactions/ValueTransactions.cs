using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Buildings.Runtime;

namespace ZombieCardSurvive.Systems.Transactions
{
    public enum ValueId { Food, TemporaryFood, Resource, Combat, Energy, Morale, MaximumMorale, ReservedFood, ReservedResource, FoodCapacity, MaximumEnergy, MaximumCombat, MoraleStage }
    public enum ValueTransactionKind { Card, Event, RoundEnd, System, Reset }

    public readonly struct ValueSnapshot
    {
        private readonly int food, temporary, resource, combat, energy, morale, maximum, reservedFood, reservedResource, capacity, maximumEnergy, maximumCombat, stage;
        private ValueSnapshot(bool capture)
        {
            food = FoodSystem.Food; temporary = FoodSystem.TemporaryFood;
            resource = ResourceSystem.Resource; combat = CombatSystem.CombatValue;
            energy = EnergySystem.CurrentEnergy; morale = MoraleSystem.CurrentMorale;
            maximum = MoraleSystem.MaximumMorale;
            reservedFood = FoodSystem.ReservedFood; reservedResource = ResourceSystem.ReservedResource;
            capacity = FoodSystem.StorageCapacity; maximumEnergy = EnergySystem.MaxEnergy;
            maximumCombat = CombatSystem.MaxCombatValue; stage = (int)MoraleSystem.CollapseStage;
        }
        public static ValueSnapshot Capture() => new ValueSnapshot(true);
        public int Get(ValueId id)
        {
            switch (id)
            {
                case ValueId.Food: return food;
                case ValueId.TemporaryFood: return temporary;
                case ValueId.Resource: return resource;
                case ValueId.Combat: return combat;
                case ValueId.Energy: return energy;
                case ValueId.Morale: return morale;
                case ValueId.MaximumMorale: return maximum;
                case ValueId.ReservedFood: return reservedFood;
                case ValueId.ReservedResource: return reservedResource;
                case ValueId.FoodCapacity: return capacity;
                case ValueId.MaximumEnergy: return maximumEnergy;
                case ValueId.MaximumCombat: return maximumCombat;
                case ValueId.MoraleStage: return stage;
                default: throw new ArgumentOutOfRangeException(nameof(id));
            }
        }
    }

    public readonly struct ValueChangeResult
    {
        public ValueChangeResult(long transactionId, ValueId id, int before, int after, string source)
        { TransactionId = transactionId; Id = id; Before = before; After = after; Source = source; }
        public long TransactionId { get; }
        public ValueId Id { get; }
        public int Before { get; }
        public int After { get; }
        public long Delta => (long)After - Before;
        public string Source { get; }
    }

    public sealed class ValueTransactionResult
    {
        internal ValueTransactionResult(ValueTransaction tx, ValueSnapshot after, bool interrupted)
        {
            Id = tx.Id; Source = tx.Source; Kind = tx.Kind; Before = tx.Before; After = after;
            Changes = new List<ValueChangeResult>(tx.Changes).AsReadOnly();
            Morale = tx.Morale == null ? null : ValueTransaction.Freeze(tx.Morale, true); Interrupted = interrupted;
            Profile = MoralePyramidSystem.Profile;
            StableThreshold = MoraleSystem.StableThreshold; CriticalThreshold = MoraleSystem.CriticalThreshold;
        }
        public long Id { get; }
        public string Source { get; }
        public ValueTransactionKind Kind { get; }
        public ValueSnapshot Before { get; }
        public ValueSnapshot After { get; }
        public IReadOnlyList<ValueChangeResult> Changes { get; }
        public MoraleBatchResult Morale { get; }
        public MoralePyramidProfileSnapshot Profile { get; }
        public int StableThreshold { get; }
        public int CriticalThreshold { get; }
        public string PrimaryMoraleLossSource
        {
            get
            {
                if (Morale == null) return Source;
                string source = Source; long severity = 0;
                foreach (var contribution in Morale.Contributions)
                {
                    long loss = Math.Min(0L, contribution.MoraleDelta) + Math.Min(0L, contribution.MaximumMoraleDelta);
                    if (loss < severity) { severity = loss; source = contribution.Source; }
                }
                return source;
            }
        }
        public bool Interrupted { get; }
        public long Delta(ValueId id) => (long)After.Get(id) - Before.Get(id);
        public bool IsCollapsed => Morale != null && Morale.ChangeResult.IsCollapsed;
        public ValueChangeResult Net(ValueId id) => new ValueChangeResult(Id, id, Before.Get(id), After.Get(id), Source);
    }

    /// <summary>One explicit operation. Execute bridges existing static APIs; it never owns values or rolls back.</summary>
    public sealed class ValueTransaction : IDisposable
    {
        private readonly List<ValueChangeResult> changes = new List<ValueChangeResult>();
        private bool completed, executing;
        private readonly long lifecycleGeneration = ValueTransactions.LifecycleGeneration;
        public ValueTransaction(string source, ValueTransactionKind kind)
        {
            ValueTransactions.EnsureInitialized();
            Id = ValueTransactions.NextId(); Source = source ?? "Unspecified"; Kind = kind;
            Before = ValueSnapshot.Capture();
        }
        public long Id { get; }
        public string Source { get; }
        public ValueTransactionKind Kind { get; }
        public ValueSnapshot Before { get; private set; }
        public IReadOnlyList<ValueChangeResult> Changes => changes;
        private readonly List<MoraleBatchResult> moraleBatches = new List<MoraleBatchResult>();
        public MoraleBatchResult Morale
        {
            get
            {
                if (moraleBatches.Count == 0) return null;
                if (moraleBatches.Count == 1) return moraleBatches[0];
                var first = moraleBatches[0].ChangeResult;
                var last = moraleBatches[moraleBatches.Count-1].ChangeResult;
                var contributions = new List<MoraleBatchContribution>();
                var transitions = new List<MoraleStageTransition>();
                long md=0, xd=0; bool collapsed=false;
                foreach(var batch in moraleBatches)
                { contributions.AddRange(batch.Contributions); transitions.AddRange(batch.ChangeResult.StageTransitions);
                  md+=batch.ChangeResult.RequestedMoraleDelta; xd+=batch.ChangeResult.RequestedMaximumMoraleDelta; collapsed|=batch.ChangeResult.DidCollapse; }
                int m=(int)Math.Max(int.MinValue,Math.Min(int.MaxValue,md)), x=(int)Math.Max(int.MinValue,Math.Min(int.MaxValue,xd));
                var change = new MoraleChangeResult(Source,first.MoraleBefore,last.MoraleAfter,first.MaximumMoraleBefore,last.MaximumMoraleAfter,
                    m,x,first.StageBefore,last.StageAfter,transitions.AsReadOnly(),first.WasCollapsed,last.IsCollapsed,collapsed);
                return new MoraleBatchResult(Source,contributions,m,x,change);
            }
        }
        public Run.RoundEndReport RoundReport { get; private set; }
        public ValueTransactionResult Result { get; private set; }
        internal void Add(ValueChangeResult change) => changes.Add(change);
        internal void OverrideBefore(ValueSnapshot value) => Before = value;
        public void AttachMorale(MoraleBatchResult result)
        {
            if (completed) throw new InvalidOperationException("Transaction is closed.");
            if (result == null) return;
            for(int i=0;i<moraleBatches.Count;i++)
                if(ReferenceEquals(moraleBatches[i].ChangeResult,result.ChangeResult)) { moraleBatches[i]=result; return; }
            moraleBatches.Add(Freeze(result));
        }
        internal static MoraleBatchResult Freeze(MoraleBatchResult result, bool copyChange = false)
        {
            var c=result.ChangeResult;
            var copy=new MoraleChangeResult(c.Source,c.MoraleBefore,c.MoraleAfter,c.MaximumMoraleBefore,c.MaximumMoraleAfter,
                c.RequestedMoraleDelta,c.RequestedMaximumMoraleDelta,c.StageBefore,c.StageAfter,
                new List<MoraleStageTransition>(c.StageTransitions).AsReadOnly(),c.WasCollapsed,c.IsCollapsed,c.DidCollapse);
            // Keep the original change identity for deduplication until the operation finishes.
            return new MoraleBatchResult(result.Source,new List<MoraleBatchContribution>(result.Contributions).AsReadOnly(),result.NetMoraleDelta,result.NetMaximumMoraleDelta,copyChange ? copy : c);
        }
        public void AttachRound(Run.RoundEndReport report) { RoundReport = report; AttachMorale(report?.MoraleSettlement); }
        public T Execute<T>(Func<T> operation)
        {
            if (lifecycleGeneration != ValueTransactions.LifecycleGeneration)
                throw new InvalidOperationException("Transaction belongs to an ended run.");
            if (completed) throw new InvalidOperationException("Transaction is closed.");
            if (executing && ValueTransactions.Executing == this) return operation();
            if (executing) throw new InvalidOperationException("Transaction is already executing elsewhere.");
            var previous = ValueTransactions.Executing;
            if (previous != null && previous != this) throw new InvalidOperationException("Nested transactions must explicitly join their owner.");
            executing = true; ValueTransactions.Executing = this;
            try { return operation(); }
            finally { ValueTransactions.Observe(); ValueTransactions.Executing = previous; executing = false; }
        }
        public void Execute(Action operation) => Execute(() => { operation(); return true; });
        public ValueTransactionResult Complete() => Finish(false);
        private ValueTransactionResult Finish(bool interrupted)
        {
            if (completed) return Result;
            if (executing) throw new InvalidOperationException("Complete only after the operation has returned.");
            completed = true;
            Result = new ValueTransactionResult(this, ValueSnapshot.Capture(), interrupted);
            if (lifecycleGeneration == ValueTransactions.LifecycleGeneration) ValueTransactions.Publish(Result);
            return Result;
        }
        public void Dispose()
        {
            if (completed) return;
            if (changes.Count == 0 && moraleBatches.Count == 0) { completed = true; return; }
            Finish(true);
        }
    }

    /// <summary>Legacy notifications remain intact. This bridge records only actual changes and publishes completed operations.</summary>
    public static class ValueTransactions
    {
        internal static long LifecycleGeneration { get; private set; }
        public static void InvalidateRun() { LifecycleGeneration++; }
        private static bool initialized;
        private static long sequence;
        private static ValueSnapshot observed;
        internal static ValueTransaction Executing { get; set; }
        public static event Action<ValueChangeResult> StateChanged;
        public static event Action<ValueTransactionResult> TransactionCompleted;
        public static event Action ResetRequested;
        public static bool IsExecuting => Executing != null;
        internal static long NextId() => ++sequence;
        public static long CurrentSequence => sequence;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void EnsureInitialized()
        {
            if (initialized) return;
            initialized = true; observed = ValueSnapshot.Capture();
            FoodSystem.StateChanged += Observe; ResourceSystem.StateChanged += Observe;
            EnergySystem.StateChanged += Observe; CombatSystem.StateChanged += Observe;
            ResourceReservationService.StateChanged += Observe;
            MoraleSystem.StateChanged += OnMorale;
        }
        private static void OnMorale(MoraleChangeResult result)
        {
            if (Executing != null)
                Executing.AttachMorale(new MoraleBatchResult(result.Source,
                    new[] { new MoraleBatchContribution(result.Source, result.RequestedMoraleDelta, result.RequestedMaximumMoraleDelta) },
                    result.RequestedMoraleDelta, result.RequestedMaximumMoraleDelta, result));
            Observe();
        }
        public static void ResetPresentation()
        {
            if (Executing != null) throw new InvalidOperationException("Cannot reset during a transaction.");
            observed = ValueSnapshot.Capture(); ResetRequested?.Invoke();
        }
        internal static void Observe()
        {
            var after = ValueSnapshot.Capture();
            var before = observed;
            observed = after;
            var owner = Executing;
            // Independent legacy mutations use a named, immediate System transaction.
            ValueTransaction standalone = null;
            for (int i = 0; i <= (int)ValueId.MoraleStage; i++)
            {
                var id = (ValueId)i;
                if (before.Get(id) == after.Get(id)) continue;
                if (owner == null && standalone == null) standalone = new ValueTransaction("System", ValueTransactionKind.System);
                var tx = owner ?? standalone;
                var change = new ValueChangeResult(tx.Id, id, before.Get(id), after.Get(id), tx.Source);
                tx.Add(change); Notify(StateChanged, change);
            }
            if (standalone != null) PublishStandalone(standalone, before, after);
        }
        private static void PublishStandalone(ValueTransaction tx, ValueSnapshot before, ValueSnapshot after)
        {
            // The operation already happened when a legacy event arrives; recover its first values from the journal.
            tx.OverrideBefore(before);
            tx.Complete();
        }
        internal static void Publish(ValueTransactionResult result) => Notify(TransactionCompleted, result);
        private static void Notify<T>(Action<T> handlers, T result)
        {
            if (handlers == null) return;
            foreach (Action<T> handler in handlers.GetInvocationList())
                try { handler(result); } catch (Exception error) { Debug.LogException(error); }
        }
    }
}
