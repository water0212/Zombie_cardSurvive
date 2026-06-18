using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZombieCardSurvive.Events.Data;
using ZombieCardSurvive.Events.Runtime;

namespace ZombieCardSurvive.Events.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class EventFlowView : MonoBehaviour
    {
        [SerializeField] private EventFlowController eventFlowController;
        [SerializeField] private GameObject root;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private Image artworkImage;
        [SerializeField] private EventChoiceButtonView[] choiceButtons;
        [SerializeField] private Button endEventButton;
        [SerializeField] private float fadeDuration = 0.2f;

        private Coroutine transitionRoutine;

        private void Awake()
        {
            if (root == null)
            {
                root = gameObject;
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (eventFlowController == null)
            {
                eventFlowController = FindObjectOfType<EventFlowController>();
            }
        }

        private void Reset()
        {
            eventFlowController = FindObjectOfType<EventFlowController>();
            root = gameObject;
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            if (eventFlowController != null)
            {
                eventFlowController.EventChanged += RefreshWithFade;
            }

            HookButtons();
            RefreshImmediate();
        }

        private void OnDisable()
        {
            if (eventFlowController != null)
            {
                eventFlowController.EventChanged -= RefreshWithFade;
            }

            UnhookButtons();
        }

        [ContextMenu("Refresh Immediate")]
        public void RefreshImmediate()
        {
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
            }

            SetAlpha(1f);
            ApplyState();
        }

        private void RefreshWithFade()
        {
            if (!isActiveAndEnabled)
            {
                ApplyState();
                return;
            }

            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
            }

            transitionRoutine = StartCoroutine(FadeToNewState());
        }

        private IEnumerator FadeToNewState()
        {
            bool wasVisible = canvasGroup == null || canvasGroup.alpha > 0.01f;
            if (wasVisible)
            {
                yield return Fade(canvasGroup != null ? canvasGroup.alpha : 1f, 0f);
            }

            ApplyState();

            if (eventFlowController == null || eventFlowController.State == EventFlowState.Hidden)
            {
                SetAlpha(0f);
                transitionRoutine = null;
                yield break;
            }

            yield return Fade(0f, 1f);
            transitionRoutine = null;
        }

        private IEnumerator Fade(float from, float to)
        {
            float duration = Mathf.Max(0.01f, fadeDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                SetAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }

            SetAlpha(to);
        }

        private void ApplyState()
        {
            if (eventFlowController == null || eventFlowController.State == EventFlowState.Hidden)
            {
                SetRootVisible(false);
                return;
            }

            SetRootVisible(true);

            if (eventFlowController.State == EventFlowState.Choosing)
            {
                ApplyChoosingState(eventFlowController.CurrentEvent);
                return;
            }

            ApplyResultState(eventFlowController.CurrentEvent, eventFlowController.SelectedChoice);
        }

        private void ApplyChoosingState(EventData eventData)
        {
            SetText(titleText, eventData != null ? eventData.DisplayTitle : string.Empty);
            SetText(descriptionText, eventData != null ? eventData.Description : string.Empty);
            SetArtwork(eventData != null ? eventData.Artwork : null);
            SetEndEventVisible(false);

            int choiceCount = eventData != null ? eventData.Choices.Count : 0;
            int buttonCount = choiceButtons != null ? choiceButtons.Length : 0;
            for (int i = 0; i < buttonCount; i++)
            {
                EventChoiceButtonView choiceButton = choiceButtons[i];
                if (choiceButton == null)
                {
                    continue;
                }

                bool visible = i < choiceCount;
                choiceButton.SetVisible(visible);
                if (visible)
                {
                    choiceButton.SetText(eventData.Choices[i].ChoiceText);
                }
            }
        }

        private void ApplyResultState(EventData eventData, EventChoiceData choiceData)
        {
            string fallbackTitle = eventData != null ? eventData.DisplayTitle : string.Empty;
            Sprite fallbackArtwork = eventData != null ? eventData.Artwork : null;

            SetText(titleText, choiceData != null && !string.IsNullOrWhiteSpace(choiceData.ResultTitle) ? choiceData.ResultTitle : fallbackTitle);
            SetText(descriptionText, choiceData != null ? choiceData.ResultDescription : string.Empty);
            SetArtwork(choiceData != null && choiceData.ResultArtwork != null ? choiceData.ResultArtwork : fallbackArtwork);
            SetEndEventVisible(true);
            SetChoicesVisible(false);
        }

        private void HookButtons()
        {
            int buttonCount = choiceButtons != null ? choiceButtons.Length : 0;
            for (int i = 0; i < buttonCount; i++)
            {
                EventChoiceButtonView choiceButton = choiceButtons[i];
                if (choiceButton == null || choiceButton.Button == null)
                {
                    continue;
                }

                int index = i;
                choiceButton.SetClickAction(() => ChooseOption(index));
            }

            if (endEventButton != null)
            {
                endEventButton.onClick.RemoveListener(EndEvent);
                endEventButton.onClick.AddListener(EndEvent);
            }
        }

        private void UnhookButtons()
        {
            int buttonCount = choiceButtons != null ? choiceButtons.Length : 0;
            for (int i = 0; i < buttonCount; i++)
            {
                EventChoiceButtonView choiceButton = choiceButtons[i];
                if (choiceButton != null)
                {
                    choiceButton.ClearClickAction();
                }
            }

            if (endEventButton != null)
            {
                endEventButton.onClick.RemoveListener(EndEvent);
            }
        }

        private void ChooseOption(int index)
        {
            if (eventFlowController != null)
            {
                eventFlowController.ChooseOption(index);
            }
        }

        private void EndEvent()
        {
            if (eventFlowController != null)
            {
                eventFlowController.EndEvent();
            }
        }

        private void SetChoicesVisible(bool visible)
        {
            if (choiceButtons == null)
            {
                return;
            }

            foreach (EventChoiceButtonView choiceButton in choiceButtons)
            {
                if (choiceButton != null)
                {
                    choiceButton.SetVisible(visible);
                }
            }
        }

        private void SetEndEventVisible(bool visible)
        {
            if (endEventButton != null)
            {
                endEventButton.gameObject.SetActive(visible);
            }
        }

        private void SetRootVisible(bool visible)
        {
            GameObject targetRoot = root != null ? root : gameObject;
            if (targetRoot != gameObject)
            {
                targetRoot.SetActive(visible);
                return;
            }

            if (!visible)
            {
                SetAlpha(0f);
            }
        }

        private void SetAlpha(float alpha)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
                canvasGroup.interactable = alpha > 0.99f;
                canvasGroup.blocksRaycasts = alpha > 0.99f;
            }
        }

        private void SetArtwork(Sprite sprite)
        {
            if (artworkImage == null)
            {
                return;
            }

            artworkImage.sprite = sprite;
            artworkImage.enabled = sprite != null;
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
