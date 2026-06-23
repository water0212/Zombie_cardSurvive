using TMPro;
using UnityEngine;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Cards.UI
{
    public class ResourceDebugView : MonoBehaviour
    {
        [SerializeField] private CardController cardController;
        [SerializeField] private TMP_Text foodText;
        [SerializeField] private TMP_Text resourceText;
        [SerializeField] private TMP_Text energyText;
        [SerializeField] private TMP_Text pendingDamageText;
        [SerializeField] private TMP_Text zombieThreatText;
        [SerializeField] private TMP_Text deckDebugText;

        private void OnEnable()
        {
            if (cardController != null)
            {
                cardController.StateChanged += Refresh;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (cardController != null)
            {
                cardController.StateChanged -= Refresh;
            }
        }

        private void Update()
        {
            Refresh();
        }

        [ContextMenu("Refresh")]
        public void Refresh()
        {
            SetText(foodText, $"{FoodSystem.GetFood()}");
            SetText(resourceText, $"{ResourceSystem.GetResource()}");

            if (cardController == null)
            {
                SetText(energyText, $"{EnergySystem.CurrentEnergy}/{EnergySystem.MaxEnergy}");
                SetText(pendingDamageText, $"{CombatSystem.PendingDamage}");
                SetText(zombieThreatText, $"{CombatSystem.ZombieThreatCount}");
                SetText(deckDebugText, string.Empty);
                return;
            }

            SetText(energyText, $"{EnergySystem.CurrentEnergy}/{EnergySystem.MaxEnergy}");
            SetText(pendingDamageText, $"{CombatSystem.PendingDamage}");
            SetText(zombieThreatText, $"{CombatSystem.ZombieThreatCount}");
            SetText(deckDebugText, BuildDeckDebugText());
        }

        private string BuildDeckDebugText()
        {
            if (cardController == null)
            {
                return string.Empty;
            }

            return $"正規抽牌:{cardController.RegularDrawPileCount}  附加:{cardController.AdditiveDrawPileCount}\n"
                + $"手牌:{cardController.HandCards.Count}  棄牌:{cardController.DiscardPile.Count}  已打出:{cardController.PlayedCards.Count}\n"
                + $"耗盡:{cardController.ExhaustedCards.Count}  補位需求:{cardController.ExhaustionRecords.Count}";
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }
    }
}
