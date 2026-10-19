using System.Collections.Generic;
using TMPro;
using UnityEngine;
using ZombieCardSurvive.Buildings.Runtime;
using ZombieCardSurvive.Cards.Data;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Run;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.UI.World
{
    /// <summary>
    /// 將既有 Runtime 系統的正式狀態投影到 Gameplay HUD。
    /// 本元件只讀取資料與格式化文字，不保存或修改任何 Gameplay 數值。
    /// </summary>
    public sealed class GameplayHudRuntimePresenter : MonoBehaviour
    {
        [Header("Runtime Sources")]
        [SerializeField] private RunPhaseController runPhaseController;
        [SerializeField] private CardController cardController;

        [Header("Progress")]
        [SerializeField] private TMP_Text dayText;
        [SerializeField] private TMP_Text phaseText;

        [Header("Values")]
        [SerializeField] private TMP_Text moraleText;
        [SerializeField] private TMP_Text moraleThresholdText;
        [SerializeField] private TMP_Text foodText;
        [SerializeField] private TMP_Text resourceText;
        [SerializeField] private TMP_Text combatText;
        [SerializeField] private TMP_Text energyText;
        [SerializeField] private bool resourceIconsApplied;

        [Header("Piles")]
        [SerializeField] private TMP_Text drawPileText;
        [SerializeField] private TMP_Text discardPileText;

        [Header("Hand")]
        [SerializeField] private List<RectTransform> handCardRoots = new List<RectTransform>();
        [SerializeField] private List<TMP_Text> handCardNameTexts = new List<TMP_Text>();
        [SerializeField] private List<TMP_Text> handCardCostTexts = new List<TMP_Text>();
        [SerializeField] private List<TMP_Text> handCardTypeTexts = new List<TMP_Text>();

        [Header("Semantic Colours")]
        [SerializeField] private Color stableColour = new Color32(132, 190, 138, 255);
        [SerializeField] private Color shakenColour = new Color32(231, 174, 82, 255);
        [SerializeField] private Color brinkColour = new Color32(226, 96, 82, 255);

        public void Configure(
            RunPhaseController phaseController,
            CardController cards,
            TMP_Text day,
            TMP_Text phase,
            TMP_Text morale,
            TMP_Text moraleThreshold,
            TMP_Text food,
            TMP_Text resource,
            TMP_Text combat,
            TMP_Text energy,
            TMP_Text drawPile,
            TMP_Text discardPile,
            IReadOnlyList<RectTransform> cardRoots,
            IReadOnlyList<TMP_Text> cardNames,
            IReadOnlyList<TMP_Text> cardCosts,
            IReadOnlyList<TMP_Text> cardTypes)
        {
            bool rebindEvents = isActiveAndEnabled;
            if (rebindEvents)
            {
                Unsubscribe();
            }

            runPhaseController = phaseController;
            cardController = cards;
            dayText = day;
            phaseText = phase;
            moraleText = morale;
            moraleThresholdText = moraleThreshold;
            foodText = food;
            resourceText = resource;
            combatText = combat;
            energyText = energy;
            drawPileText = drawPile;
            discardPileText = discardPile;
            CopyReferences(cardRoots, handCardRoots);
            CopyReferences(cardNames, handCardNameTexts);
            CopyReferences(cardCosts, handCardCostTexts);
            CopyReferences(cardTypes, handCardTypeTexts);

            if (rebindEvents)
            {
                Subscribe();
                RefreshAll();
            }
        }

        private void Awake()
        {
            ResolveRuntimeSources();
        }

        private void OnEnable()
        {
            ResolveRuntimeSources();
            Subscribe();
            RefreshAll();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        [ContextMenu("Refresh HUD")]
        public void RefreshAll()
        {
            RefreshProgress();
            RefreshValues();
            RefreshCards();
        }

        private void ResolveRuntimeSources()
        {
            if (runPhaseController == null)
            {
                runPhaseController = FindObjectOfType<RunPhaseController>();
            }

            if (cardController == null)
            {
                cardController = FindObjectOfType<CardController>();
            }
        }

        private void Subscribe()
        {
            Unsubscribe();
            if (runPhaseController != null)
            {
                runPhaseController.PhaseChanged += HandlePhaseChanged;
            }

            if (cardController != null)
            {
                cardController.StateChanged += RefreshAll;
            }

            FoodSystem.StateChanged += RefreshAll;
            ResourceSystem.StateChanged += RefreshAll;
            EnergySystem.StateChanged += RefreshAll;
            ResourceReservationService.StateChanged += RefreshAll;
            CombatSystem.StateChanged += RefreshAll;
            MoraleSystem.StateChanged += HandleMoraleChanged;
            MoralePyramidSystem.ActiveStageChanged += HandleMoraleStageChanged;
        }

        private void Unsubscribe()
        {
            if (runPhaseController != null)
            {
                runPhaseController.PhaseChanged -= HandlePhaseChanged;
            }

            if (cardController != null)
            {
                cardController.StateChanged -= RefreshAll;
            }

            FoodSystem.StateChanged -= RefreshAll;
            ResourceSystem.StateChanged -= RefreshAll;
            EnergySystem.StateChanged -= RefreshAll;
            ResourceReservationService.StateChanged -= RefreshAll;
            CombatSystem.StateChanged -= RefreshAll;
            MoraleSystem.StateChanged -= HandleMoraleChanged;
            MoralePyramidSystem.ActiveStageChanged -= HandleMoraleStageChanged;
        }

        private void RefreshProgress()
        {
            if (runPhaseController == null)
            {
                SetText(dayText, "第 — 天");
                SetText(phaseText, "時段尚未初始化");
                return;
            }

            int day = runPhaseController.CurrentPhase == RunPhase.Event
                || runPhaseController.CurrentPhase == RunPhase.BuildingResolution
                ? runPhaseController.CurrentEventDay
                : Mathf.Max(1, runPhaseController.RoundIndex);

            SetText(dayText, $"第 {day} 天");
            SetText(phaseText, BuildPhaseText(runPhaseController));
        }

        private void RefreshValues()
        {
            MoraleCollapseStage stage = MoraleSystem.CollapseStage;
            if (moraleText != null)
            {
                moraleText.text = $"士氣  {MoraleSystem.CurrentMorale}/{MoraleSystem.MaximumMorale}";
                moraleText.color = GetStageColour(stage);
            }

            SetText(moraleThresholdText, BuildNextThresholdText(stage));
            SetText(foodText, BuildFoodText());
            SetText(resourceText, BuildResourceText());
            SetText(combatText, $"{(resourceIconsApplied ? "" : "戰鬥 ")}{CombatSystem.CombatValue}/{CombatSystem.MaxCombatValue}");
            SetText(energyText, $"{(resourceIconsApplied ? "" : "能量 ")}{EnergySystem.CurrentEnergy}/{EnergySystem.MaxEnergy}");
        }

        private void RefreshCards()
        {
            if (cardController == null)
            {
                SetText(drawPileText, "抽牌堆 —");
                SetText(discardPileText, "棄牌堆 —");
                HideAllHandCards();
                return;
            }

            SetText(drawPileText, $"抽牌堆 {cardController.DrawPile.Count}");
            SetText(discardPileText, $"棄牌堆 {cardController.DiscardPile.Count}");

            int slotCount = handCardRoots.Count;
            for (int index = 0; index < slotCount; index++)
            {
                CardRuntime card = index < cardController.HandCards.Count
                    ? cardController.HandCards[index]
                    : null;
                bool hasCard = card != null && card.Data != null;
                if (handCardRoots[index] != null)
                {
                    handCardRoots[index].gameObject.SetActive(hasCard);
                }

                if (!hasCard)
                {
                    continue;
                }

                string displayName = string.IsNullOrWhiteSpace(card.Data.DisplayName)
                    ? card.Data.name
                    : card.Data.DisplayName;
                if (card.Data.HasLimitedUses)
                {
                    displayName += $"  ×{card.RemainingUses}";
                }

                SetListText(handCardNameTexts, index, displayName);
                SetListText(handCardCostTexts, index, card.EffectiveEnergyCost.ToString());
                SetListText(handCardTypeTexts, index, BuildCardTypeMark(card.Data.EffectiveCardType));
            }
        }

        private static string BuildPhaseText(RunPhaseController controller)
        {
            switch (controller.CurrentPhase)
            {
                case RunPhase.Event:
                    return "事件處理中";
                case RunPhase.BuildingResolution:
                    return "基地整備中";
                case RunPhase.RoundEnd:
                    return "黑夜結算中";
                case RunPhase.Failed:
                    return "基地已崩潰";
                case RunPhase.Turn:
                default:
                    int phaseIndex = Mathf.Abs(controller.TurnIndex - 1) % 3;
                    string[] labels = { "白天", "下午", "黑夜" };
                    for (int index = 0; index < labels.Length; index++)
                    {
                        if (index == phaseIndex)
                        {
                            labels[index] = $"【{labels[index]}】";
                        }
                    }
                    return string.Join(" → ", labels);
            }
        }

        private static string BuildNextThresholdText(MoraleCollapseStage stage)
        {
            switch (stage)
            {
                case MoraleCollapseStage.Stable:
                    return $"下一階段 < {MoraleSystem.StableThreshold}";
                case MoraleCollapseStage.Shaken:
                    return $"下一階段 < {MoraleSystem.CriticalThreshold}";
                default:
                    return "下一階段 = 0";
            }
        }

        private string BuildFoodText()
        {
            string value = $"{(resourceIconsApplied ? "" : "食物 ")}{FoodSystem.Food}/{FoodSystem.StorageCapacity}";
            if (FoodSystem.TemporaryFood > 0)
            {
                value += $"  +{FoodSystem.TemporaryFood}臨時";
            }
            if (FoodSystem.ReservedFood > 0)
            {
                value += $"  鎖{FoodSystem.ReservedFood}";
            }
            return value;
        }

        private string BuildResourceText()
        {
            string value = $"{(resourceIconsApplied ? "" : "資源 ")}{ResourceSystem.AvailableResource}";
            if (ResourceSystem.ReservedResource > 0)
            {
                value += $"  鎖{ResourceSystem.ReservedResource}";
            }
            return value;
        }

        private Color GetStageColour(MoraleCollapseStage stage)
        {
            switch (stage)
            {
                case MoraleCollapseStage.Shaken:
                    return shakenColour;
                case MoraleCollapseStage.Brink:
                    return brinkColour;
                default:
                    return stableColour;
            }
        }

        private static string BuildCardTypeMark(CardType type)
        {
            switch (type)
            {
                case CardType.Food:
                    return "食";
                case CardType.Resource:
                    return "材";
                case CardType.Combat:
                    return "戰";
                case CardType.Explore:
                    return "探";
                case CardType.Build:
                    return "建";
                case CardType.Zombie:
                    return "屍";
                default:
                    return "特";
            }
        }

        private void HideAllHandCards()
        {
            for (int index = 0; index < handCardRoots.Count; index++)
            {
                if (handCardRoots[index] != null)
                {
                    handCardRoots[index].gameObject.SetActive(false);
                }
            }
        }

        private void HandlePhaseChanged(RunPhase phase)
        {
            RefreshAll();
        }

        private void HandleMoraleChanged(MoraleChangeResult result)
        {
            RefreshAll();
        }

        private void HandleMoraleStageChanged(MoraleStageSnapshot stage)
        {
            RefreshAll();
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }

        private static void SetListText(IReadOnlyList<TMP_Text> targets, int index, string value)
        {
            if (targets != null && index >= 0 && index < targets.Count)
            {
                SetText(targets[index], value);
            }
        }

        private static void CopyReferences<T>(IReadOnlyList<T> source, List<T> destination)
        {
            destination.Clear();
            if (source == null)
            {
                return;
            }

            for (int index = 0; index < source.Count; index++)
            {
                destination.Add(source[index]);
            }
        }
    }
}
