using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Cards.Data;

namespace ZombieCardSurvive.Systems
{
    public static class CombatSystem
    {
        private static readonly List<ZombieThreatRuntime> zombieThreats = new List<ZombieThreatRuntime>();
        private static int pendingDamage;
        private static int maxCombatValue = 9;
        private static int overcapLossPercent = 50;
        private static int nextZombieRuntimeId = 1;

        public static event Action StateChanged;

        public static int PendingDamage => pendingDamage;
        public static int CombatValue => pendingDamage;
        public static int MaxCombatValue => maxCombatValue;
        public static int OvercapLossPercent => overcapLossPercent;
        public static int ZombieThreatCount => zombieThreats.Count;
        public static IReadOnlyList<ZombieThreatRuntime> ZombieThreats => zombieThreats;

        public static void Configure(int maxValue, int lossPercent)
        {
            maxCombatValue = Mathf.Max(0, maxValue);
            overcapLossPercent = Mathf.Clamp(lossPercent, 0, 100);
            NotifyStateChanged();
        }

        public static void AddPendingDamage(int amount)
        {
            pendingDamage = Mathf.Max(0, pendingDamage + Mathf.Max(0, amount));
            NotifyStateChanged();
        }

        public static bool SpendPendingDamage(int amount)
        {
            int cost = Mathf.Max(0, amount);
            if (pendingDamage < cost)
            {
                return false;
            }

            pendingDamage -= cost;
            NotifyStateChanged();
            return true;
        }

        public static void ClearPendingDamage()
        {
            pendingDamage = 0;
            NotifyStateChanged();
        }

        public static void AddZombieThreat(int amount)
        {
            int count = Mathf.Max(0, amount);
            for (int i = 0; i < count; i++)
            {
                SpawnZombieThreat(null);
            }
        }

        public static bool RemoveZombieThreat(int amount)
        {
            int value = Mathf.Max(0, amount);
            if (zombieThreats.Count < value)
            {
                return false;
            }

            for (int i = 0; i < value; i++)
            {
                RemoveZombieThreatAt(zombieThreats.Count - 1);
            }

            NotifyStateChanged();
            return true;
        }

        public static ZombieThreatRuntime SpawnZombieThreat(ZombieCardData sourceCard)
        {
            ZombieThreatRuntime threat = new ZombieThreatRuntime(nextZombieRuntimeId++, sourceCard);
            zombieThreats.Add(threat);
            NotifyStateChanged();
            return threat;
        }

        public static bool TryToggleZombieKillMark(ZombieThreatRuntime threat)
        {
            if (threat == null || !zombieThreats.Contains(threat))
            {
                return false;
            }

            if (threat.IsMarkedForKill)
            {
                pendingDamage = Mathf.Max(0, pendingDamage + threat.ReservedCombatValue);
                threat.ClearKillMark();
                NotifyStateChanged();
                return true;
            }

            int requiredCombatValue = threat.CurrentHealth;
            if (pendingDamage < requiredCombatValue)
            {
                return false;
            }

            pendingDamage -= requiredCombatValue;
            threat.MarkForKill(requiredCombatValue);
            NotifyStateChanged();
            return true;
        }

        public static int ResolveMarkedZombieKills()
        {
            int killedCount = 0;
            for (int i = zombieThreats.Count - 1; i >= 0; i--)
            {
                ZombieThreatRuntime threat = zombieThreats[i];
                if (threat == null || !threat.IsMarkedForKill)
                {
                    continue;
                }

                zombieThreats.RemoveAt(i);
                killedCount++;
            }

            if (killedCount > 0)
            {
                NotifyStateChanged();
            }

            return killedCount;
        }

        public static int GetSurvivingZombieAttackPower()
        {
            int attackPower = 0;
            foreach (ZombieThreatRuntime threat in zombieThreats)
            {
                if (threat != null)
                {
                    attackPower += Mathf.Max(0, threat.Attack);
                }
            }

            return attackPower;
        }

        public static int ApplyOvercapDecay()
        {
            if (pendingDamage <= maxCombatValue)
            {
                return 0;
            }

            int before = pendingDamage;
            int overcap = pendingDamage - maxCombatValue;
            int keptPercent = 100 - overcapLossPercent;
            int keptOvercap = Mathf.FloorToInt(overcap * keptPercent / 100f);
            pendingDamage = maxCombatValue + Mathf.Max(0, keptOvercap);
            int loss = before - pendingDamage;

            if (loss > 0)
            {
                NotifyStateChanged();
            }

            return loss;
        }

        public static void Reset(int maxValue = 9, int lossPercent = 50)
        {
            pendingDamage = 0;
            zombieThreats.Clear();
            maxCombatValue = Mathf.Max(0, maxValue);
            overcapLossPercent = Mathf.Clamp(lossPercent, 0, 100);
            nextZombieRuntimeId = 1;
            NotifyStateChanged();
        }

        private static void RemoveZombieThreatAt(int index)
        {
            if (index < 0 || index >= zombieThreats.Count)
            {
                return;
            }

            ZombieThreatRuntime threat = zombieThreats[index];
            if (threat != null && threat.IsMarkedForKill)
            {
                pendingDamage = Mathf.Max(0, pendingDamage + threat.ReservedCombatValue);
                threat.ClearKillMark();
            }

            zombieThreats.RemoveAt(index);
        }

        private static void NotifyStateChanged()
        {
            StateChanged?.Invoke();
        }
    }
}
