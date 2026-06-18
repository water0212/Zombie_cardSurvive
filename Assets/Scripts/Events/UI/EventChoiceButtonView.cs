using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ZombieCardSurvive.Events.UI
{
    public class EventChoiceButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text labelText;

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
    }
}
