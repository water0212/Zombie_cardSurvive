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
                return;
            }

            SetText(energyText, $"{EnergySystem.CurrentEnergy}/{EnergySystem.MaxEnergy}");
            SetText(pendingDamageText, $"{CombatSystem.PendingDamage}");
            SetText(zombieThreatText, $"{CombatSystem.ZombieThreatCount}");
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
