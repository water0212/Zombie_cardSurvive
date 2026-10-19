#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using ZombieCardSurvive.Effects.Data;
using ZombieCardSurvive.Events.Data;
using ZombieCardSurvive.Threats.Editor;

namespace ZombieCardSurvive.Events.Editor
{
    public static class EventContentV1Specification
    {
        public const int CanonicalSimulationDays = 30;
        public const int CanonicalSimulationSeed = 14014;
        public const int DistributionSimulationRuns = 100;
        public const string HordePendingFlagId = "FLAG_HORDE_PENDING";

        private static readonly EventContentRunFlagSpec[] flagSpecs =
        {
            new EventContentRunFlagSpec(
                HordePendingFlagId,
                "屍群逼近中",
                "屍群逼近事件建立的待處理屍群資格。數值大於 0 時，屍群來臨可以進入事件池。",
                0,
                0,
                1)
        };

        private static readonly EventContentSpec[] eventSpecs =
        {
            Event(
                2, "EVT-001", "食物受潮",
                "倉庫角落傳來霉味，幾批食物已被潮氣污染。",
                EventMoodCategory.Negative, EventContentCategory.Resource, "小",
                3, 0, 15f, true, 0, 5,
                "倉庫等級達到 2 時不會發生。",
                "需要現存食物百分比與倉庫等級共同參與效果公式。",
                Choice("丟棄食物", "清理受潮食物", "依現存食物比例與倉庫等級計算食物損失。"),
                Choice("置之不理", "士氣受挫", "士氣減少 5。"))
                .WithCondition(
                    "倉庫尚未升至等級 2",
                    EventConditionType.BuildingLevel,
                    EventValueComparison.LessThan,
                    2,
                    "building_storage"),

            Event(
                3, "EVT-002", "飢餓倖存者",
                "一群飢餓的倖存者來到基地外，等待你決定是否伸出援手。",
                EventMoodCategory.Mixed, EventContentCategory.Survivor, "中",
                1, 0, 40f, true, 0, 8,
                "可用食物達食物上限 50% 時權重乘以 5。",
                "需要倖存者人數、每日需求、幫助次數、食物比例權重與連動效果公式。",
                Choice("加入我們", "接納倖存者", "每日需求依已幫助人數增加，最大能量增加 1。"),
                Choice("選擇幫助", "留下補給", "消耗部分現有食物，並依實際消耗量增加士氣。"),
                Choice("忽視請求", "關上大門", "士氣減少 5。")),

            Event(
                4, "EVT-003", "夜間低溫",
                "寒流灌入基地，所有人都需要更多熱量撐過這晚。",
                EventMoodCategory.Mixed, EventContentCategory.Resource, "小",
                3, 0, 30f, true, 0, 5,
                string.Empty,
                string.Empty,
                Choice(
                    "增添暖爐", "暖爐點燃", "支付 {value:0} 資源。",
                    Value(ValueEffectType.SpendResource, 1, EventScaleSource.MaxEnergy, EventPaymentMode.ChoiceCost)),
                Choice(
                    "夜晚的熱湯", "熱湯上桌", "支付 {value:0} 食物，士氣增加 {value:1}。",
                    Value(ValueEffectType.SpendFood, 2, EventScaleSource.MaxEnergy, EventPaymentMode.ChoiceCost),
                    Value(ValueEffectType.AddMorale, 1, EventScaleSource.MaxEnergy)),
                Choice(
                    "無能為力", "寒夜難熬", "士氣減少 {value:0}。",
                    Value(ValueEffectType.ReduceMorale, 1, EventScaleSource.MaxEnergy))),

            Event(
                5, "EVT-004", "發現補給品",
                "巡邏隊找到一個半埋在瓦礫下的補給箱。",
                EventMoodCategory.Positive, EventContentCategory.Resource, "小",
                5, 0, 40f, true, 0, 7,
                "三個外觀相同的補給箱選項會隨機打亂。",
                "需要卡包獎勵、特定卡牌入庫、容量百分比與選項洗牌。",
                Choice("開啟補給箱（卡包）", "取得卡包", "隨機取得食物卡包或資源卡包。"),
                Choice("開啟補給箱（資源）", "取得補給", "依食物上限增加食物與資源。"),
                Choice("開啟補給箱（特定戰鬥卡）", "找到消防斧", "取得 2 張消防斧。")),

            Event(
                6, "EVT-005", "工具損耗",
                "一批工具在維修時報廢，短期內很難補回。",
                EventMoodCategory.Negative, EventContentCategory.Resource, "中",
                4, 0, 30f, true, 0, 4,
                string.Empty,
                "需要隨機結果、下一輪效率修正與卡牌類型禁用狀態。",
                Choice("修理", "更換零件", "支付等同最大能量的資源。"),
                Choice("嘗試敲打", "勉強維持", "50% 無事發生；50% 下一輪效率減少 1。"),
                Choice("回收零件", "拆解工具", "下一輪無法打出食物與資源卡，並獲得 2 資源。")),

            Event(
                7, "EVT-006", "臨時修繕",
                "外圍路障出現缺口，現在修還來得及。",
                EventMoodCategory.Mixed, EventContentCategory.Resource, "中",
                4, 0, 30f, true, 0, 5,
                string.Empty,
                "需要下一輪戰鬥卡效果修正，以及扣除現有戰鬥值的效果。",
                Choice("加固外牆", "完成加固", "支付最大能量兩倍的資源，獲得等同最大能量的戰鬥值。"),
                Choice("加強警戒", "提高戒備", "支付等同最大能量的資源，下一輪戰鬥卡戰鬥值增加 1。"),
                Choice("簡單填補", "勉強補上", "戰鬥值減少 2。")),

            Event(
                8, "EVT-007", "遠處槍聲",
                "槍聲從幾條街外傳來，聲音把東西引過來了。",
                EventMoodCategory.Negative, EventContentCategory.Zombie, "中高",
                5, 0, 25f, true, 0, 5,
                "事件發生後，屍群逼近的權重增加 50。",
                "需要指定下一事件、忽略冷卻與事件間動態權重。",
                Choice("派人查看", "發現求援者", "支付資源與食物，下一次事件固定為飢餓倖存者。"),
                Choice("加強警戒", "守住基地", "士氣減少 5。"),
                Choice("忽視", "威脅靠近", "新增 1 張殭屍卡進入棄牌堆。")),

            Event(
                9, "EVT-008", "屍群逼近",
                "屍群在街口聚集，基地周圍的動線變得危險。",
                EventMoodCategory.Mixed, EventContentCategory.Zombie, "低",
                8, 0, 70f, true, 0, 8,
                string.Empty,
                string.Empty,
                Choice(
                    "加強警戒", "整備防線", "支付 {value:0} 資源，獲得 {value:1} 戰鬥值；屍群將在之後抵達。",
                    Value(ValueEffectType.SpendResource, 2, EventScaleSource.MaxEnergy),
                    Value(ValueEffectType.AddPendingDamage, 1, EventScaleSource.MaxEnergy),
                    RunFlag(EventStateEffectType.SetRunFlagValue, HordePendingFlagId, 1)),
                Choice(
                    "派人偵查", "掌握動向", "支付 {value:0} 食物，取得下次屍群即將抵達的情報。",
                    Value(ValueEffectType.SpendFood, 2, EventScaleSource.MaxEnergy),
                    RunFlag(EventStateEffectType.SetRunFlagValue, HordePendingFlagId, 1)),
                Choice(
                    "靜待來臨", "保持沉默", "保持沉默，屍群將在之後抵達。",
                    RunFlag(EventStateEffectType.SetRunFlagValue, HordePendingFlagId, 1))),

            Event(
                10, "EVT-009", "路線錯判",
                "舊地圖上的捷徑已經坍塌，探索隊不得不重新規劃路線。",
                EventMoodCategory.Mixed, EventContentCategory.Effect, "低",
                4, 0, 30f, true, 0, 5,
                string.Empty,
                "需要下一輪禁止使用探索卡的跨輪狀態。",
                Choice("繼續前進", "付出代價", "士氣減少最大能量的三倍。"),
                Choice("撤退", "中止探索", "下一輪無法打出探索卡。")),

            Event(
                11, "EVT-010", "可疑信號",
                "斷續的無線電信號在夜裡重複播放，來源始終無法定位。",
                EventMoodCategory.Positive, EventContentCategory.Story, "高",
                10, 20, 50f, false, 1, 0,
                string.Empty,
                "需要屍群難度、計分倍率、屍群時間情報與劇情旗標。",
                Choice("接收信號", "保留信號", "下一次屍群難度與分數翻倍，並顯示屍群時間。"),
                Choice("摧毀無線電", "切斷信號", "無事發生。")),

            Event(
                12, "EVT-011", "破損路障",
                "基地外圍的臨時路障被撞開一角，原本的探索路線已不再安全。",
                EventMoodCategory.Negative, EventContentCategory.Effect, "低",
                5, 0, 30f, true, 0, 6,
                string.Empty,
                "需要下一輪探索卡費用倍率的跨輪狀態。",
                Choice("臨時改道", "繞行危險路段", "下一輪探索卡費用翻倍。"),
                Choice("製作工具", "修復路線", "支付等同最大能量的資源。")),

            Event(
                13, "EVT-012", "廢棄地圖",
                "一張被雨水泡皺的城市地圖仍能看出幾條補給路線。",
                EventMoodCategory.Positive, EventContentCategory.Story, "高",
                10, 0, 50f, true, 0, 10,
                "本輪打出探索卡時權重增加 50，冷卻減少 1。",
                "需要地區發現、探索進度、探索回合紀錄與 WorldThreat 正式系統。",
                Choice("查看新地點", "發現地點", "新增可供探索卡發現的地點。"),
                Choice("確認舊地點", "補完地圖", "提高既有地點的探索值。"),
                Choice("標註威脅點", "更新威脅", "降低等同最大能量的世界威脅。")),

            Event(
                14, "EVT-013", "雲朗天清",
                "久違的晴空讓基地裡的人稍微放鬆了下來。",
                EventMoodCategory.Positive, EventContentCategory.Effect, "低",
                1, 0, 10f, true, 0, 0,
                "助長士氣只能在食物高於食物上限 50% 時選擇。",
                "需要食物容量百分比、依實際支付量計算士氣，以及下一輪效率修正。",
                Choice("助長士氣", "共享存糧", "支付食物上限 30% 的食物，依實際支付量增加士氣。"),
                Choice("休假放鬆", "短暫休息", "下一輪效率減少 1，並增加等同最大能量的士氣。"),
                Choice("一如既往", "維持日常", "無事發生。")),

            Event(
                15, "EVT-014", "屍群來臨",
                "密集的低吼與腳步聲從城區逼近，屍群已經抵達基地周邊。",
                EventMoodCategory.Negative, EventContentCategory.Zombie, "高",
                11, 0, 50f, true, 0, 0,
                "每次屍群逼近後才可出現一次，並在等待期間每天增加 50 權重。",
                string.Empty,
                Choice(
                    "背水一戰", "迎擊屍群", "屍群抵達：生成 {value:0} 隻殭屍威脅。",
                    SpawnHorde(HordeContentV1Specification.StandardProfileId),
                    RunFlag(EventStateEffectType.ClearRunFlag, HordePendingFlagId, 0)))
                .WithCondition(
                    "已有待處理屍群",
                    EventConditionType.RunFlagValue,
                    EventValueComparison.AtLeast,
                    1,
                    HordePendingFlagId)
                .WithWeightModifier(
                    "屍群等待權重",
                    EventWeightModifierSource.RunFlagDaysSinceSet,
                    HordePendingFlagId,
                    50f,
                    1)
        };

        public static IReadOnlyList<EventContentRunFlagSpec> RunFlags => flagSpecs;
        public static IReadOnlyList<EventContentSpec> Events => eventSpecs;

        private static EventContentSpec Event(
            int sourceRow,
            string eventId,
            string title,
            string description,
            EventMoodCategory mood,
            EventContentCategory content,
            string impactLevel,
            int earliestDay,
            int latestDay,
            float weight,
            bool canRepeat,
            int maximumAppearances,
            int cooldownDays,
            string specialRules,
            string implementationNotes,
            params EventContentChoiceSpec[] choices)
        {
            EventImplementationStatus status = string.IsNullOrWhiteSpace(implementationNotes)
                ? EventImplementationStatus.Ready
                : EventImplementationStatus.RequiresSupportSystem;
            return new EventContentSpec(
                sourceRow,
                eventId,
                title,
                description,
                mood,
                content,
                impactLevel,
                earliestDay,
                latestDay,
                weight,
                canRepeat,
                maximumAppearances,
                cooldownDays,
                specialRules,
                status,
                implementationNotes,
                choices);
        }

        private static EventContentChoiceSpec Choice(
            string choiceText,
            string resultTitle,
            string resultDescription,
            params EventContentValueEffectSpec[] effects)
        {
            return new EventContentChoiceSpec(choiceText, resultTitle, resultDescription, effects);
        }

        private static EventContentValueEffectSpec Value(
            ValueEffectType type,
            int amount,
            EventScaleSource scaleSource = EventScaleSource.Fixed,
            EventPaymentMode paymentMode = EventPaymentMode.ChoiceCost)
        {
            return new EventContentValueEffectSpec(type, amount, scaleSource, 1f, paymentMode);
        }

        private static EventContentValueEffectSpec RunFlag(
            EventStateEffectType type,
            string flagId,
            int amount)
        {
            return new EventContentValueEffectSpec(type, flagId, amount);
        }

        private static EventContentValueEffectSpec SpawnHorde(
            string hordeProfileId,
            int eventBonusBudget = 0)
        {
            return new EventContentValueEffectSpec(
                ThreatEffectType.SpawnHorde,
                hordeProfileId,
                eventBonusBudget);
        }
    }

    public sealed class EventContentSpec
    {
        private readonly List<EventContentConditionSpec> conditions = new List<EventContentConditionSpec>();
        private readonly List<EventContentWeightModifierSpec> weightModifiers =
            new List<EventContentWeightModifierSpec>();

        public EventContentSpec(
            int sourceRow,
            string eventId,
            string title,
            string description,
            EventMoodCategory mood,
            EventContentCategory content,
            string impactLevel,
            int earliestDay,
            int latestDay,
            float weight,
            bool canRepeat,
            int maximumAppearances,
            int cooldownDays,
            string specialRules,
            EventImplementationStatus implementationStatus,
            string implementationNotes,
            IReadOnlyList<EventContentChoiceSpec> choices)
        {
            SourceRow = sourceRow;
            EventId = eventId;
            Title = title;
            Description = description;
            Mood = mood;
            Content = content;
            ImpactLevel = impactLevel;
            EarliestDay = earliestDay;
            LatestDay = latestDay;
            Weight = weight;
            CanRepeat = canRepeat;
            MaximumAppearances = maximumAppearances;
            CooldownDays = cooldownDays;
            SpecialRules = specialRules;
            ImplementationStatus = implementationStatus;
            ImplementationNotes = implementationNotes;
            Choices = choices ?? Array.Empty<EventContentChoiceSpec>();
        }

        public int SourceRow { get; }
        public string EventId { get; }
        public string Title { get; }
        public string Description { get; }
        public EventMoodCategory Mood { get; }
        public EventContentCategory Content { get; }
        public string ImpactLevel { get; }
        public int EarliestDay { get; }
        public int LatestDay { get; }
        public float Weight { get; }
        public bool CanRepeat { get; }
        public int MaximumAppearances { get; }
        public int CooldownDays { get; }
        public string SpecialRules { get; }
        public EventImplementationStatus ImplementationStatus { get; }
        public string ImplementationNotes { get; }
        public IReadOnlyList<EventContentChoiceSpec> Choices { get; }
        public IReadOnlyList<EventContentConditionSpec> Conditions => conditions;
        public IReadOnlyList<EventContentWeightModifierSpec> WeightModifiers => weightModifiers;
        public bool IncludeInPlayablePool => ImplementationStatus == EventImplementationStatus.Ready;

        public EventContentSpec WithCondition(
            string displayName,
            EventConditionType conditionType,
            EventValueComparison comparison,
            int requiredValue,
            string targetId = "")
        {
            conditions.Add(new EventContentConditionSpec(
                displayName,
                conditionType,
                comparison,
                requiredValue,
                targetId));
            return this;
        }

        public EventContentSpec WithWeightModifier(
            string displayName,
            EventWeightModifierSource source,
            string targetId,
            float weightPerUnit,
            int ignoredUnitsBeforeBonus = 0,
            float minimumBonus = 0f,
            float maximumBonus = 0f)
        {
            weightModifiers.Add(new EventContentWeightModifierSpec(
                displayName,
                source,
                targetId,
                ignoredUnitsBeforeBonus,
                weightPerUnit,
                minimumBonus,
                maximumBonus));
            return this;
        }
    }

    public sealed class EventContentRunFlagSpec
    {
        public EventContentRunFlagSpec(
            string flagId,
            string displayName,
            string description,
            int defaultValue,
            int minimumValue,
            int maximumValue)
        {
            FlagId = flagId;
            DisplayName = displayName;
            Description = description;
            DefaultValue = defaultValue;
            MinimumValue = minimumValue;
            MaximumValue = maximumValue;
        }

        public string FlagId { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public int DefaultValue { get; }
        public int MinimumValue { get; }
        public int MaximumValue { get; }
    }

    public sealed class EventContentChoiceSpec
    {
        public EventContentChoiceSpec(
            string choiceText,
            string resultTitle,
            string resultDescription,
            IReadOnlyList<EventContentValueEffectSpec> effects)
        {
            ChoiceText = choiceText;
            ResultTitle = resultTitle;
            ResultDescription = resultDescription;
            Effects = effects ?? Array.Empty<EventContentValueEffectSpec>();
        }

        public string ChoiceText { get; }
        public string ResultTitle { get; }
        public string ResultDescription { get; }
        public IReadOnlyList<EventContentValueEffectSpec> Effects { get; }
    }

    public sealed class EventContentValueEffectSpec
    {
        public EventContentValueEffectSpec(
            ValueEffectType type,
            int amount,
            EventScaleSource scaleSource,
            float scalingStrength,
            EventPaymentMode paymentMode)
        {
            Category = GameEffectCategory.Value;
            Type = type;
            Amount = amount;
            ScaleSource = scaleSource;
            ScalingStrength = scalingStrength;
            PaymentMode = paymentMode;
        }

        public EventContentValueEffectSpec(
            EventStateEffectType eventStateType,
            string targetId,
            int amount)
        {
            Category = GameEffectCategory.EventState;
            EventStateType = eventStateType;
            TargetId = targetId ?? string.Empty;
            Amount = amount;
            ScaleSource = EventScaleSource.Fixed;
            ScalingStrength = 1f;
            PaymentMode = EventPaymentMode.ChoiceCost;
        }

        public EventContentValueEffectSpec(
            ThreatEffectType threatType,
            string hordeProfileId,
            int eventBonusBudget)
        {
            Category = GameEffectCategory.Threat;
            ThreatType = threatType;
            HordeProfileId = hordeProfileId ?? string.Empty;
            EventBonusBudget = eventBonusBudget;
            ScaleSource = EventScaleSource.Fixed;
            ScalingStrength = 1f;
            PaymentMode = EventPaymentMode.ChoiceCost;
        }

        public GameEffectCategory Category { get; }
        public ValueEffectType Type { get; }
        public EventStateEffectType EventStateType { get; }
        public ThreatEffectType ThreatType { get; }
        public string TargetId { get; } = string.Empty;
        public string HordeProfileId { get; } = string.Empty;
        public int EventBonusBudget { get; }
        public int Amount { get; }
        public EventScaleSource ScaleSource { get; }
        public float ScalingStrength { get; }
        public EventPaymentMode PaymentMode { get; }
    }

    public sealed class EventContentConditionSpec
    {
        public EventContentConditionSpec(
            string displayName,
            EventConditionType conditionType,
            EventValueComparison comparison,
            int requiredValue,
            string targetId)
        {
            DisplayName = displayName;
            ConditionType = conditionType;
            Comparison = comparison;
            RequiredValue = requiredValue;
            TargetId = targetId;
        }

        public string DisplayName { get; }
        public EventConditionType ConditionType { get; }
        public EventValueComparison Comparison { get; }
        public int RequiredValue { get; }
        public string TargetId { get; }
    }

    public sealed class EventContentWeightModifierSpec
    {
        public EventContentWeightModifierSpec(
            string displayName,
            EventWeightModifierSource source,
            string targetId,
            int ignoredUnitsBeforeBonus,
            float weightPerUnit,
            float minimumBonus,
            float maximumBonus)
        {
            DisplayName = displayName;
            Source = source;
            TargetId = targetId;
            IgnoredUnitsBeforeBonus = ignoredUnitsBeforeBonus;
            WeightPerUnit = weightPerUnit;
            MinimumBonus = minimumBonus;
            MaximumBonus = maximumBonus;
        }

        public string DisplayName { get; }
        public EventWeightModifierSource Source { get; }
        public string TargetId { get; }
        public int IgnoredUnitsBeforeBonus { get; }
        public float WeightPerUnit { get; }
        public float MinimumBonus { get; }
        public float MaximumBonus { get; }
    }
}
#endif
