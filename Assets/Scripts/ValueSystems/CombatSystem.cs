using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Cards.Data;

namespace ZombieCardSurvive.Systems
{
    public static class CombatSystem
    {
        public const int MaxZombieThreatBatchSize = 256;

        private static readonly List<ZombieThreatRuntime> zombieThreats = new List<ZombieThreatRuntime>();
        private static int pendingDamage;
        private static int maxCombatValue = 9;
        private static int overcapLossPercent = 50;
        private static int nextZombieRuntimeId = 1;
        private static int currentRoundIndex;
        private static int defaultFirstConsequenceRoundEnd = 1;

        public static event Action StateChanged;

        public static int PendingDamage => pendingDamage;
        public static int CombatValue => pendingDamage;
        public static int MaxCombatValue => maxCombatValue;
        public static int OvercapLossPercent => overcapLossPercent;
        public static int ZombieThreatCount => zombieThreats.Count;
        public static IReadOnlyList<ZombieThreatRuntime> ZombieThreats => zombieThreats;
        public static int CurrentRoundIndex => currentRoundIndex;
        public static int DefaultFirstConsequenceRoundEnd => defaultFirstConsequenceRoundEnd;

        /// <summary>進入玩家可操作的新一輪；之後生成的殭屍可在本輪輪末首次結算。</summary>
        public static void BeginRound(int roundIndex)
        {
            currentRoundIndex = Mathf.Max(1, roundIndex);
            defaultFirstConsequenceRoundEnd = currentRoundIndex;
        }

        /// <summary>
        /// 進入輪末揭露階段；從此刻到下一輪開始前生成的殭屍，
        /// 必須等下一輪輪末才取得完整反應窗口。
        /// </summary>
        public static void BeginRoundEnd(int roundIndex)
        {
            currentRoundIndex = Mathf.Max(0, roundIndex);
            defaultFirstConsequenceRoundEnd = Mathf.Max(1, currentRoundIndex + 1);
        }

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
            ZombieThreatRuntime threat = new ZombieThreatRuntime(
                nextZombieRuntimeId++,
                sourceCard,
                defaultFirstConsequenceRoundEnd);
            zombieThreats.Add(threat);
            NotifyStateChanged();
            return threat;
        }

        /// <summary>檢查整批殭屍卡是否能在不產生部分結果的情況下生成。</summary>
        public static bool CanSpawnZombieThreats(IReadOnlyList<ZombieCardData> sourceCards)
        {
            if (sourceCards == null
                || sourceCards.Count <= 0
                || sourceCards.Count > MaxZombieThreatBatchSize)
            {
                return false;
            }

            for (int index = 0; index < sourceCards.Count; index++)
            {
                if (sourceCards[index] == null)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 驗證整批輸入後一次生成所有殭屍，並只發送一次 StateChanged。
        /// 無效輸入不會修改場上狀態。
        /// </summary>
        public static IReadOnlyList<ZombieThreatRuntime> SpawnZombieThreats(
            IReadOnlyList<ZombieCardData> sourceCards)
        {
            if (!CanSpawnZombieThreats(sourceCards))
            {
                return Array.Empty<ZombieThreatRuntime>();
            }

            List<ZombieThreatRuntime> spawned = new List<ZombieThreatRuntime>(sourceCards.Count);
            for (int index = 0; index < sourceCards.Count; index++)
            {
                ZombieThreatRuntime threat = new ZombieThreatRuntime(
                    nextZombieRuntimeId++,
                    sourceCards[index],
                    defaultFirstConsequenceRoundEnd);
                zombieThreats.Add(threat);
                spawned.Add(threat);
            }

            NotifyStateChanged();
            return spawned.AsReadOnly();
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
            currentRoundIndex = 0;
            defaultFirstConsequenceRoundEnd = 1;
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
