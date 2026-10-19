using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Buildings.Data;
using ZombieCardSurvive.Buildings.Runtime;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Effects.Data;
using ZombieCardSurvive.Events.Data;
using ZombieCardSurvive.Run;
using ZombieCardSurvive.Systems;
using ZombieCardSurvive.Threats.Data;
using ZombieCardSurvive.Threats.Runtime;

namespace ZombieCardSurvive.Events.Runtime
{
    public class EventFlowController : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private RunPhaseController runPhaseController;
        [SerializeField] private CardController cardController;
        [SerializeField] private BuildingSystem buildingSystem;

        [Header("Event schedule")]
        [SerializeField] private List<EventData> eventPool = new List<EventData>();
        [SerializeField] private EventSchedulerSettingsData schedulerSettings;
        [SerializeField] private EventMagnitudeSettingsData magnitudeSettings;
        [SerializeField] private ThreatSettingsData threatSettings;
        [Tooltip("Optional asset shown when no scheduled event is legal. If empty, a built-in quiet-day result is shown.")]
        [SerializeField] private EventData fallbackEvent;

        [Header("Random seed")]
        [SerializeField] private bool useFixedRandomSeed;
        [SerializeField] private int fixedRandomSeed = 12345;

        private readonly EventScheduler scheduler = new EventScheduler();
        private readonly EventMagnitudeRuntime magnitudeRuntime = new EventMagnitudeRuntime();
        private readonly ThreatRuntime threatRuntime = new ThreatRuntime();
        private readonly RunFlagRuntimeStore runFlagStore = new RunFlagRuntimeStore();
        private readonly Dictionary<string, int> customConditionValues =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private IEventSequenceSource eventSequenceSource;
        private bool schedulerInitialized;
        private int survivorCount = 1;
        private int permanentSlotUpgradeCount;
        private int otherPermanentDevelopmentModifiers;
        private int worldThreatTier = 1;
        private int hordeBudgetTier = 1;
        private int storyThreatModifier;
        private float buildingNoise;
        private float baseEventAttentionModifier;
        private float stealthModifier;
        private bool useManualThreatScaleTiers;
        private ThreatSnapshot lastLockedThreatSnapshot;

        public event Action EventChanged;

        /// <summary>目前事件畫面的狀態。</summary>
        public EventFlowState State { get; private set; } = EventFlowState.Hidden;
        /// <summary>本日實際顯示的事件；內建保底日沒有 Event Asset，因此會是 null。</summary>
        public EventData CurrentEvent { get; private set; }
        /// <summary>玩家已確認的選項；尚未選擇或保底日時為 null。</summary>
        public EventChoiceData SelectedChoice { get; private set; }
        /// <summary>本日鎖定後的事件資料；UI 與結算都使用這個 Runtime。</summary>
        public ResolvedEventRuntime CurrentResolvedEvent { get; private set; }
        /// <summary>玩家選擇的解析後選項。</summary>
        public ResolvedEventChoiceRuntime SelectedResolvedChoice { get; private set; }
        public EventChoiceExecutionResult LastChoiceExecutionResult { get; private set; }
        /// <summary>本局事件出現次數、最近日期與負面事件日期。</summary>
        public EventRunHistory EventHistory => scheduler.History;
        /// <summary>最近一次一般或接管來源的抽選結果，供除錯面板讀取。</summary>
        public EventSelectionResult LastSelectionResult => scheduler.LastSelectionResult;
        /// <summary>目前事件對應的遊戲日，第一個事件為第 1 天。</summary>
        public int CurrentEventDay { get; private set; }
        /// <summary>本局事件排程器實際使用的亂數種子。</summary>
        public int CurrentRunSeed => scheduler.Seed;
        public EventMagnitudeSnapshot LastMagnitudeSnapshot => magnitudeRuntime.LastSnapshot;
        public ThreatSnapshot LastThreatSnapshot => lastLockedThreatSnapshot ?? threatRuntime.LastSnapshot;
        public RunFlagRuntimeStore RunFlags => runFlagStore;
        public bool IsNeutralFallback { get; private set; }
        public string NeutralFallbackTitle => "平靜的一天";
        public string NeutralFallbackDescription => "今天沒有特別的事情發生。";

        private void Reset()
        {
            runPhaseController = FindObjectOfType<RunPhaseController>();
            cardController = FindObjectOfType<CardController>();
            buildingSystem = FindObjectOfType<BuildingSystem>();
        }

        private void Awake()
        {
            ResolveReferences();
        }

        /// <summary>依 Inspector 設定建立新事件局並清除舊歷史。</summary>
        [ContextMenu("Initialize Event Run")]
        public void InitializeRun()
        {
            int seed = useFixedRandomSeed
                ? fixedRandomSeed
                : unchecked(Environment.TickCount ^ GetInstanceID());
            InitializeRun(seed);
        }

        /// <summary>以指定種子建立新事件局，主要供測試與重播使用。</summary>
        public void InitializeRun(int seed)
        {
            scheduler.InitializeRun(eventPool, schedulerSettings, seed);
            magnitudeRuntime.InitializeRun(magnitudeSettings);
            threatRuntime.InitializeRun(threatSettings);
            runFlagStore.Reset();
            customConditionValues.Clear();
            survivorCount = 1;
            permanentSlotUpgradeCount = 0;
            otherPermanentDevelopmentModifiers = 0;
            worldThreatTier = 1;
            hordeBudgetTier = 1;
            storyThreatModifier = 0;
            buildingNoise = 0f;
            baseEventAttentionModifier = 0f;
            stealthModifier = 0f;
            useManualThreatScaleTiers = false;
            lastLockedThreatSnapshot = null;
            schedulerInitialized = true;
            CurrentEventDay = 0;
            CurrentEvent = null;
            SelectedChoice = null;
            CurrentResolvedEvent = null;
            SelectedResolvedChoice = null;
            LastChoiceExecutionResult = null;
            IsNeutralFallback = false;
            State = EventFlowState.Hidden;
        }

        /// <summary>依目前 RunPhase 的日期開始每日事件。</summary>
        [ContextMenu("Begin Event Phase")]
        public void BeginEventPhase()
        {
            int day = runPhaseController != null ? runPhaseController.CurrentEventDay : Mathf.Max(1, CurrentEventDay + 1);
            BeginEventPhase(day);
        }

        /// <summary>抽選並顯示指定日的唯一事件。</summary>
        public void BeginEventPhase(int day)
        {
            EnsureSchedulerInitialized();
            CurrentEventDay = Mathf.Max(1, day);
            CurrentEvent = SelectEvent(CurrentEventDay);
            SelectedChoice = null;
            SelectedResolvedChoice = null;
            LastChoiceExecutionResult = null;

            int activeBuildingLevelSum = GetActiveBuildingLevelSum();
            lastLockedThreatSnapshot = CaptureThreatSnapshot(
                CurrentEventDay,
                activeBuildingLevelSum);
            EventMagnitudeSnapshot magnitudeSnapshot = magnitudeRuntime.Capture(
                CreateMagnitudeInputSnapshot(
                    CurrentEventDay,
                    activeBuildingLevelSum,
                    lastLockedThreatSnapshot.WorldThreatTier));
            CurrentResolvedEvent = ResolvedEventRuntime.Create(
                CurrentEvent,
                magnitudeSnapshot,
                lastLockedThreatSnapshot,
                CurrentRunSeed,
                CurrentEventDay);

            bool hasChoices = CurrentResolvedEvent != null && CurrentResolvedEvent.Choices.Count > 0;
            State = hasChoices ? EventFlowState.Choosing : EventFlowState.Result;
            NotifyChanged();
        }

        /// <summary>選擇目前事件的一個選項並套用既有效果。</summary>
        public bool ChooseOption(int optionIndex)
        {
            if (State != EventFlowState.Choosing || CurrentResolvedEvent == null)
            {
                return false;
            }

            if (optionIndex < 0 || optionIndex >= CurrentResolvedEvent.Choices.Count)
            {
                return false;
            }

            ResolvedEventChoiceRuntime candidate = CurrentResolvedEvent.Choices[optionIndex];
            EventChoiceExecutionResult executionResult = null;
            if (candidate == null || !candidate.TryExecute(
                    GameEffectContext.ForEvent(cardController, runFlagStore, CurrentEventDay),
                    out executionResult))
            {
                LastChoiceExecutionResult = executionResult;
                NotifyChanged();
                return false;
            }

            SelectedResolvedChoice = candidate;
            SelectedChoice = candidate.Source;
            LastChoiceExecutionResult = executionResult;
            State = EventFlowState.Result;
            NotifyChanged();
            return true;
        }

        public EventChoiceAvailability GetChoiceAvailability(int optionIndex)
        {
            if (State != EventFlowState.Choosing || CurrentResolvedEvent == null
                || optionIndex < 0 || optionIndex >= CurrentResolvedEvent.Choices.Count)
            {
                return new EventChoiceAvailability(
                    false,
                    EventChoiceBlockReason.InvalidConfiguration,
                    0,
                    FoodSystem.AvailableFood,
                    0,
                    ResourceSystem.AvailableResource,
                    "The requested event choice is unavailable.");
            }

            ResolvedEventChoiceRuntime choice = CurrentResolvedEvent.Choices[optionIndex];
            return choice != null
                ? choice.EvaluateAvailability(GameEffectContext.ForEvent(cardController, runFlagStore, CurrentEventDay))
                : new EventChoiceAvailability(
                    false,
                    EventChoiceBlockReason.InvalidConfiguration,
                    0,
                    FoodSystem.AvailableFood,
                    0,
                    ResourceSystem.AvailableResource,
                    "The resolved event choice is missing.");
        }

        /// <summary>關閉結果並把流程交還 RunPhaseController。</summary>
        [ContextMenu("End Event")]
        public void EndEvent()
        {
            if (State != EventFlowState.Result)
            {
                return;
            }

            Hide();
            runPhaseController?.StartRoundFromEvent();
        }

        public void Hide()
        {
            State = EventFlowState.Hidden;
            CurrentEvent = null;
            SelectedChoice = null;
            CurrentResolvedEvent = null;
            SelectedResolvedChoice = null;
            LastChoiceExecutionResult = null;
            IsNeutralFallback = false;
            NotifyChanged();
        }

        /// <summary>設定會完全接管一般事件池的來源，例如終局事件包。</summary>
        public void SetEventSequenceSource(IEventSequenceSource source)
        {
            eventSequenceSource = source;
        }

        /// <summary>設定可供事件條件查詢的本局旗標。</summary>
        public void SetRunFlag(string flagId, bool enabled)
        {
            if (string.IsNullOrWhiteSpace(flagId))
            {
                return;
            }

            string normalizedId = flagId.Trim();
            if (enabled)
            {
                runFlagStore.SetValue(normalizedId, 1, Mathf.Max(1, CurrentEventDay));
            }
            else
            {
                runFlagStore.Clear(normalizedId, Mathf.Max(1, CurrentEventDay));
            }
        }

        public bool HasRunFlag(string flagId)
        {
            return !string.IsNullOrWhiteSpace(flagId) && runFlagStore.IsActive(flagId.Trim());
        }

        public int GetRunFlagValue(string flagId)
        {
            return !string.IsNullOrWhiteSpace(flagId) ? runFlagStore.GetValue(flagId.Trim()) : 0;
        }

        public int GetRunFlagDaysSinceSet(string flagId, int day)
        {
            return !string.IsNullOrWhiteSpace(flagId)
                ? runFlagStore.GetDaysSinceFirstSet(flagId.Trim(), day)
                : 0;
        }

        /// <summary>設定尚未擁有正式系統 API 的自訂條件數值。</summary>
        public void SetCustomConditionValue(string valueId, int value)
        {
            if (!string.IsNullOrWhiteSpace(valueId))
            {
                customConditionValues[valueId.Trim()] = value;
            }
        }

        public int GetCustomConditionValue(string valueId)
        {
            return !string.IsNullOrWhiteSpace(valueId)
                && customConditionValues.TryGetValue(valueId.Trim(), out int value)
                    ? value
                    : 0;
        }

        /// <summary>設定目前總人口；初始人口不計入 DevelopmentPoints。</summary>
        public void SetSurvivorCount(int count)
        {
            survivorCount = Mathf.Max(1, count);
        }

        public void SetPermanentSlotUpgradeCount(int count)
        {
            permanentSlotUpgradeCount = Mathf.Max(0, count);
        }

        public void SetOtherPermanentDevelopmentModifiers(int amount)
        {
            otherPermanentDevelopmentModifiers = Mathf.Max(0, amount);
        }

        /// <summary>暫由外部威脅系統提供等級；正式系統完成前預設為 1。</summary>
        public void SetThreatScaleTiers(int worldThreat, int hordeBudget)
        {
            worldThreatTier = Mathf.Max(1, worldThreat);
            hordeBudgetTier = Mathf.Max(1, hordeBudget);
            useManualThreatScaleTiers = true;
        }

        public void ClearThreatScaleTierOverride()
        {
            useManualThreatScaleTiers = false;
        }

        public void SetStoryThreatModifier(int amount)
        {
            storyThreatModifier = amount;
        }

        public void SetBaseAttentionModifiers(
            float buildingNoiseValue,
            float baseEventAttentionValue,
            float stealthValue)
        {
            buildingNoise = Mathf.Max(0f, buildingNoiseValue);
            baseEventAttentionModifier = baseEventAttentionValue;
            stealthModifier = Mathf.Max(0f, stealthValue);
        }

        private EventData SelectEvent(int day)
        {
            IsNeutralFallback = false;

            if (eventSequenceSource != null && eventSequenceSource.IsActive)
            {
                if (eventSequenceSource.TryGetNextEvent(day, out EventData sequenceEvent) && sequenceEvent != null)
                {
                    scheduler.RecordExternalAppearance(sequenceEvent, day);
                    return sequenceEvent;
                }

                Debug.LogWarning($"Active event sequence did not provide an event for day {day}. Using neutral fallback.", this);
                return SelectFallback();
            }

            EventSelectionContext context = CreateSelectionContext(day);
            if (scheduler.TrySelectAndRecord(context, out EventSelectionResult result))
            {
                return result.SelectedEvent;
            }

            int rejectedCount = result != null && result.Evaluations != null ? result.Evaluations.Count : 0;
            Debug.LogWarning($"No legal daily event was found for day {day} ({rejectedCount} catalog entries checked). Using neutral fallback.", this);
            return SelectFallback();
        }

        private EventData SelectFallback()
        {
            IsNeutralFallback = true;
            return fallbackEvent;
        }

        private EventSelectionContext CreateSelectionContext(int day)
        {
            return new EventSelectionContext(
                day,
                MoraleSystem.Morale,
                FoodSystem.AvailableFood,
                ResourceSystem.AvailableResource,
                EnergySystem.MaxEnergy,
                CombatSystem.ZombieThreatCount,
                ResolveActiveBuildingLevel,
                HasRunFlag,
                GetCustomConditionValue,
                runFlagStore);
        }

        private ThreatSnapshot CaptureThreatSnapshot(int day, int activeBuildingLevelSum)
        {
            ThreatSnapshot calculated = threatRuntime.Capture(new ThreatInputSnapshot(
                day,
                Mathf.Max(0, survivorCount - 1),
                activeBuildingLevelSum,
                permanentSlotUpgradeCount,
                otherPermanentDevelopmentModifiers,
                storyThreatModifier,
                buildingNoise,
                baseEventAttentionModifier,
                stealthModifier));
            if (!useManualThreatScaleTiers)
            {
                return calculated;
            }

            return new ThreatSnapshot(
                calculated.Input,
                calculated.TimeThreat,
                calculated.DevelopmentThreat,
                calculated.WorldThreat,
                worldThreatTier,
                calculated.WorldThreatBand,
                calculated.BaseAttention,
                calculated.BaseAttentionBand);
        }

        private EventMagnitudeInputSnapshot CreateMagnitudeInputSnapshot(
            int day,
            int activeBuildingLevelSum,
            int resolvedWorldThreatTier)
        {
            return new EventMagnitudeInputSnapshot(
                day,
                survivorCount,
                Mathf.Max(0, survivorCount - 1),
                activeBuildingLevelSum,
                permanentSlotUpgradeCount,
                otherPermanentDevelopmentModifiers,
                EnergySystem.MaxEnergy,
                FoodSystem.StorageCapacity,
                FoodSystem.StorageCapacityBonus,
                resolvedWorldThreatTier,
                hordeBudgetTier);
        }

        private int GetActiveBuildingLevelSum()
        {
            if (buildingSystem == null || buildingSystem.Runtimes == null)
            {
                return 0;
            }

            int levelSum = 0;
            foreach (BuildingRuntime runtime in buildingSystem.Runtimes.Values)
            {
                if (runtime != null && runtime.CurrentLevel > 0)
                {
                    levelSum += runtime.CurrentLevel;
                }
            }

            return levelSum;
        }

        private int ResolveActiveBuildingLevel(string buildingId)
        {
            BuildingRuntime runtime = buildingSystem != null ? buildingSystem.GetBuilding(buildingId) : null;
            return runtime != null
                && (runtime.State == BuildingState.Active || runtime.State == BuildingState.Upgrading)
                    ? runtime.CurrentLevel
                    : 0;
        }

        private void EnsureSchedulerInitialized()
        {
            if (!schedulerInitialized)
            {
                InitializeRun();
            }
        }

        private void ResolveReferences()
        {
            if (runPhaseController == null)
            {
                runPhaseController = FindObjectOfType<RunPhaseController>();
            }

            if (cardController == null)
            {
                cardController = FindObjectOfType<CardController>();
            }

            if (buildingSystem == null)
            {
                buildingSystem = FindObjectOfType<BuildingSystem>();
            }
        }

        private void NotifyChanged()
        {
            EventChanged?.Invoke();
        }
    }
}
