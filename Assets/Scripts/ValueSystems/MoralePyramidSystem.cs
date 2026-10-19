using System;

namespace ZombieCardSurvive.Systems
{
    /// <summary>
    /// 本局士氣金字塔 Profile 的 Runtime 權威。Profile Asset 只在新 Run 建立 Snapshot 時讀取；
    /// 之後的階段替換、輪末資源損失與能量修正都只使用 Snapshot。
    /// </summary>
    public static class MoralePyramidSystem
    {
        private static MoralePyramidProfileSnapshot profile;
        private static MoraleStageSnapshot activeStage;
        private static int pendingMaximumEnergyModifier;
        private static bool hasPendingMaximumEnergyModifier;
        private static int lastResourceLossRound;

        public static event Action<MoraleStageSnapshot> ActiveStageChanged;

        public static bool IsInitialized => profile != null && activeStage != null;
        public static MoralePyramidProfileSnapshot Profile => profile;
        public static string ActiveProfileId => profile != null ? profile.ProfileId : string.Empty;
        public static MoraleStageSnapshot ActiveStage => activeStage;
        public static MoraleCollapseStage ActiveCollapseStage =>
            activeStage != null ? activeStage.Stage : MoraleCollapseStage.Stable;
        public static bool HasPendingMaximumEnergyModifier => hasPendingMaximumEnergyModifier;

        public static bool TryPrepareRun(
            MoralePyramidProfileData profileData,
            out string error)
        {
            Reset();
            if (profileData == null)
            {
                error = "Morale Pyramid Profile is not assigned.";
                return false;
            }

            if (!profileData.TryCreateSnapshot(out MoralePyramidProfileSnapshot snapshot, out error))
            {
                return false;
            }

            profile = snapshot;
            activeStage = profile.GetStage(MoraleSystem.CollapseStage);
            pendingMaximumEnergyModifier = activeStage.MaximumEnergyModifier;
            hasPendingMaximumEnergyModifier =
                EnergySystem.ProfileMaximumEnergyModifier != pendingMaximumEnergyModifier;
            MoraleSystem.StageChanged -= HandleMoraleStageChanged;
            MoraleSystem.StageChanged += HandleMoraleStageChanged;
            ActiveStageChanged?.Invoke(activeStage);
            error = null;
            return true;
        }

        public static void Reset()
        {
            MoraleSystem.StageChanged -= HandleMoraleStageChanged;
            profile = null;
            activeStage = null;
            pendingMaximumEnergyModifier = 0;
            hasPendingMaximumEnergyModifier = false;
            lastResourceLossRound = 0;
            EnergySystem.SetProfileMaximumEnergyModifier(0);
        }

        /// <summary>
        /// 在 Turn 補充能量前提交待處理修正。跨階當下不會回頭扣除目前 Turn 的能量。
        /// </summary>
        public static bool CommitPendingTurnEffects()
        {
            if (!IsInitialized || !hasPendingMaximumEnergyModifier)
            {
                return false;
            }

            EnergySystem.SetProfileMaximumEnergyModifier(pendingMaximumEnergyModifier);
            hasPendingMaximumEnergyModifier = false;
            return true;
        }

        /// <summary>依輪末指定結算點的有效階段扣除一次資源；同一 Round 重複呼叫不重扣。</summary>
        public static ValueConsumptionResult ResolveRoundEndResourceLoss(int roundIndex)
        {
            if (!IsInitialized
                || roundIndex <= 0
                || roundIndex <= lastResourceLossRound)
            {
                return new ValueConsumptionResult(0, 0);
            }

            lastResourceLossRound = roundIndex;
            int requested = Math.Max(0, activeStage.ResourceLossPerRound);
            return ResourceSystem.ConsumeResourceAllowShortage(requested);
        }

        private static void HandleMoraleStageChanged(
            MoraleStageTransition transition,
            MoraleChangeResult result)
        {
            if (!IsInitialized || result == null)
            {
                return;
            }

            MoraleCollapseStage targetStage = result.StageAfter;
            if (targetStage <= activeStage.Stage)
            {
                return;
            }

            activeStage = profile.GetStage(targetStage);
            pendingMaximumEnergyModifier = activeStage.MaximumEnergyModifier;
            hasPendingMaximumEnergyModifier =
                EnergySystem.ProfileMaximumEnergyModifier != pendingMaximumEnergyModifier;
            ActiveStageChanged?.Invoke(activeStage);
        }
    }
}
