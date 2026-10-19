using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using ZombieCardSurvive.Events.Runtime;

namespace ZombieCardSurvive.Events.UI
{
    public class EventChoiceButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text unavailableReasonText;
        [SerializeField] private Image foodCostIcon;
        [SerializeField] private TMP_Text foodCostValue;
        [SerializeField] private Image resourceCostIcon;
        [SerializeField] private TMP_Text resourceCostValue;
        [SerializeField] private GameObject foodCostGroup;
        [SerializeField] private GameObject resourceCostGroup;

        private UnityAction currentClickAction;

        public Button Button => button;

        private void Reset()
        {
            button = GetComponent<Button>();
            labelText = GetComponentInChildren<TMP_Text>(true);
        }

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (labelText == null)
            {
                labelText = GetComponentInChildren<TMP_Text>(true);
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void SetText(string value)
        {
            if (labelText != null)
            {
                labelText.text = value;
            }
        }

        public void SetAvailability(string value, EventChoiceAvailability availability)
        {
            bool isAvailable = availability != null && availability.IsAvailable;
            string reason = isAvailable ? string.Empty : BuildUnavailableReason(availability);

            if (button != null)
            {
                button.interactable = isAvailable;
            }

            if (costText != null)
            {
                costText.text = EventPresentationFormatter.BuildChoiceCost(availability);
                costText.color = isAvailable
                    ? new Color32(77, 66, 50, 255)
                    : new Color32(137, 42, 35, 255);
            }

            bool iconCosts = foodCostIcon != null && foodCostIcon.sprite != null && foodCostValue != null
                && resourceCostIcon != null && resourceCostIcon.sprite != null && resourceCostValue != null;
            int food = availability != null ? availability.RequiredFood : 0;
            int resource = availability != null ? availability.RequiredResource : 0;
            SetCost(foodCostIcon, foodCostValue, food, iconCosts, isAvailable);
            SetCost(resourceCostIcon, resourceCostValue, resource, iconCosts, isAvailable);
            if (foodCostGroup != null) foodCostGroup.SetActive(iconCosts && food > 0);
            if (resourceCostGroup != null) resourceCostGroup.SetActive(iconCosts && resource > 0);
            if (costText != null) costText.gameObject.SetActive(!iconCosts || food <= 0 && resource <= 0);

            if (unavailableReasonText != null)
            {
                SetText(value);
                unavailableReasonText.text = reason;
                unavailableReasonText.gameObject.SetActive(!string.IsNullOrWhiteSpace(reason));
            }
            else if (labelText != null)
            {
                labelText.text = string.IsNullOrWhiteSpace(reason)
                    ? value
                    : $"{value}\n<size=70%><color=#B86D65>{reason}</color></size>";
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }

        private static void SetCost(Image icon, TMP_Text value, int amount, bool configured, bool available)
        {
            bool visible = configured && amount > 0;
            if (icon != null)
            {
                icon.gameObject.SetActive(visible);
                icon.color = new Color(1f, 1f, 1f, available ? 1f : 0.45f);
            }
            if (value != null)
            {
                value.gameObject.SetActive(visible);
                value.text = $"−{amount}";
                value.color = available ? new Color32(77, 66, 50, 255) : new Color32(137, 42, 35, 255);
            }
        }

        public void SetUnavailableReason(string reason)
        {
            if (unavailableReasonText != null)
            {
                unavailableReasonText.text = reason ?? string.Empty;
                unavailableReasonText.gameObject.SetActive(!string.IsNullOrWhiteSpace(reason));
            }
        }

        public void SetClickAction(UnityAction action)
        {
            ClearClickAction();
            currentClickAction = action;

            if (button != null && currentClickAction != null)
            {
                button.onClick.AddListener(currentClickAction);
            }
        }

        public void ClearClickAction()
        {
            if (button != null && currentClickAction != null)
            {
                button.onClick.RemoveListener(currentClickAction);
            }

            currentClickAction = null;
        }

        private static string BuildUnavailableReason(EventChoiceAvailability availability)
        {
            if (availability == null)
            {
                return "\u76ee\u524d\u7121\u6cd5\u57f7\u884c";
            }

            if (availability.HasFoodShortage && availability.HasResourceShortage)
            {
                return $"\u6c38\u4e45\u98df\u7269 {availability.AvailableFood}/{availability.RequiredFood}\uff1b"
                    + $"\u8cc7\u6e90 {availability.AvailableResource}/{availability.RequiredResource}";
            }

            switch (availability.BlockReason)
            {
                case EventChoiceBlockReason.InsufficientPermanentFood:
                    return $"\u6c38\u4e45\u98df\u7269\u4e0d\u8db3\uff1a\u9700\u8981 {availability.RequiredFood}\uff0c\u53ef\u7528 {availability.AvailableFood}";
                case EventChoiceBlockReason.InsufficientResource:
                    return $"\u8cc7\u6e90\u4e0d\u8db3\uff1a\u9700\u8981 {availability.RequiredResource}\uff0c\u53ef\u7528 {availability.AvailableResource}";
                case EventChoiceBlockReason.InvalidConfiguration:
                    return "\u4e8b\u4ef6\u8cc7\u6599\u8a2d\u5b9a\u932f\u8aa4";
                default:
                    return "\u76ee\u524d\u7121\u6cd5\u57f7\u884c";
            }
        }
    }
}
