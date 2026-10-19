using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZombieCardSurvive.Buildings.Data;
using ZombieCardSurvive.Buildings.Runtime;

namespace ZombieCardSurvive.Buildings.UI
{
    public class CardUpgradeView : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private Button upgradeButton;

        private BuildingSystem system;
        private BuildingRuntime building;
        private CardUpgradeInteractionData data;

        public void Bind(BuildingSystem owner, BuildingRuntime buildingRuntime, CardUpgradeInteractionData interaction)
        {
            system = owner;
            building = buildingRuntime;
            data = interaction;
            HookButton();
            Refresh();
        }

        private void OnDestroy()
        {
            if (upgradeButton != null)
            {
                upgradeButton.onClick.RemoveListener(ApplyUpgrade);
            }
        }

        public void Refresh()
        {
            if (data == null)
            {
                gameObject.SetActive(false);
                return;
            }

            string sourceName = data.SourceCard != null ? data.SourceCard.DisplayName : "-";
            string upgradedName = data.UpgradedCard != null ? data.UpgradedCard.DisplayName : "-";
            SetText(titleText, data.DisplayName);
            SetText(descriptionText, string.IsNullOrWhiteSpace(data.Description) ? $"{sourceName} -> {upgradedName}" : data.Description);
            SetText(costText, $"成本 食物 {data.Cost.Food} / 資源 {data.Cost.Resource}");
            if (building != null && building.State == BuildingState.Upgrading)
                SetText(costText, $"成本 食物 {data.Cost.Food} / 資源 {data.Cost.Resource}\n升級期間暫不可強化卡牌（既有規則）");

            bool completed = building != null && building.IsCardUpgradeCompleted(data);
            if (upgradeButton != null)
            {
                upgradeButton.interactable = !completed && CanUpgrade();
                TMP_Text label = upgradeButton.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = completed ? "已強化" : "強化";
                }
            }
        }

        private void ApplyUpgrade()
        {
            if (!CanUpgrade())
            {
                return;
            }

            system?.TryApplyCardUpgrade(building.Data.BuildingId, data.InteractionId);
            Refresh();
        }

        private void HookButton()
        {
            if (upgradeButton == null)
            {
                return;
            }

            upgradeButton.onClick.RemoveListener(ApplyUpgrade);
            upgradeButton.onClick.AddListener(ApplyUpgrade);
        }

        private bool CanUpgrade()
        {
            // Preserve the existing command's Active-only rule; do not change settlement here.
            return system != null && system.CanSubmitActions && building?.Data != null
                && data != null && building.State == BuildingState.Active
                && building.IsInteractionUnlocked(data) && !building.IsCardUpgradeCompleted(data);
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
