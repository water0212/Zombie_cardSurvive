using System;
using System.Collections.Generic;

namespace ZombieCardSurvive.Systems.Transactions
{
    public interface IGameValue
    {
        ValueId Id { get; }
        int Current { get; }
        bool Set(int value, ValueTransaction transaction);
        bool Change(int delta, ValueTransaction transaction);
        event Action<ValueChangeResult> StateChanged;
        event Action<ValueTransactionResult> TransactionCompleted;
    }
    public interface ISpendableValue : IGameValue
    {
        bool CanSpend(int amount);
        bool TrySpend(int amount, ValueTransaction transaction);
    }

    /// <summary>No duplicate state. Adapters delegate to existing authoritative systems.</summary>
    public static class GameValues
    {
        public static readonly ISpendableValue Food = new Spendable(ValueId.Food, () => FoodSystem.Food,
            () => FoodSystem.AvailableFood, FoodSystem.AddFood, FoodSystem.SpendFood);
        public static readonly ISpendableValue Resource = new Spendable(ValueId.Resource, () => ResourceSystem.Resource,
            () => ResourceSystem.AvailableResource, ResourceSystem.AddResource, ResourceSystem.SpendResource);
        public static readonly ISpendableValue Combat = new Spendable(ValueId.Combat, () => CombatSystem.CombatValue,
            () => CombatSystem.CombatValue, CombatSystem.AddPendingDamage, CombatSystem.SpendPendingDamage);
        public static readonly ISpendableValue Energy = new Spendable(ValueId.Energy, () => EnergySystem.CurrentEnergy,
            () => EnergySystem.CurrentEnergy, EnergySystem.AddEnergy, EnergySystem.SpendEnergy);
        public static readonly IReadOnlyList<IGameValue> All = Array.AsReadOnly(new IGameValue[] { Food, Resource, Combat, Energy });

        private sealed class Spendable : ISpendableValue
        {
            private readonly Func<int> read, available;
            private readonly Action<int> add;
            private readonly Func<int, bool> spend;
            private event Action<ValueChangeResult> changed;
            private event Action<ValueTransactionResult> completed;
            public event Action<ValueTransactionResult> TransactionCompleted
            {
                add { if (completed == null) ValueTransactions.TransactionCompleted += ForwardCompleted; completed += value; }
                remove { completed -= value; if (completed == null) ValueTransactions.TransactionCompleted -= ForwardCompleted; }
            }
            private void ForwardCompleted(ValueTransactionResult result)
            {
                foreach (var change in result.Changes)
                    if (change.Id == Id) { completed?.Invoke(result); return; }
            }
            public Spendable(ValueId id, Func<int> read, Func<int> available, Action<int> add, Func<int, bool> spend)
            { Id = id; this.read = read; this.available = available; this.add = add; this.spend = spend; }
            public ValueId Id { get; }
            public int Current => read();
            public event Action<ValueChangeResult> StateChanged
            {
                add { if (changed == null) { ValueTransactions.EnsureInitialized(); ValueTransactions.StateChanged += Forward; } changed += value; }
                remove { changed -= value; if (changed == null) ValueTransactions.StateChanged -= Forward; }
            }
            private void Forward(ValueChangeResult result) { if (result.Id == Id) changed?.Invoke(result); }
            public bool CanSpend(int amount) => amount >= 0 && available() >= amount;
            public bool TrySpend(int amount, ValueTransaction transaction)
            {
                if (transaction == null) throw new ArgumentNullException(nameof(transaction));
                return transaction.Execute(() => CanSpend(amount) && spend(amount));
            }
            public bool Change(int delta, ValueTransaction transaction)
            {
                if (transaction == null) throw new ArgumentNullException(nameof(transaction));
                if (delta == int.MinValue) return false;
                return transaction.Execute(() => { if (delta < 0) return CanSpend(-delta) && spend(-delta); add(delta); return true; });
            }
            public bool Set(int value, ValueTransaction transaction)
            {
                long delta = (long)value - Current;
                if (value < 0 || delta < int.MinValue || delta > int.MaxValue) return false;
                return Change((int)delta, transaction);
            }
        }
    }
}
