using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Cards.UI.Zombies
{
    public class ZombieThreatView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private TMP_Text attackText;
        [SerializeField] private Button healthButton;
        [SerializeField] private Button selectButton;
        [SerializeField] private Image artworkImage;
        [SerializeField] private GameObject markedForKillIndicator;

        private ZombieThreatRuntime threat;
        private ZombieThreatViewController controller;

        public ZombieThreatRuntime Threat => threat;

        private void OnEnable()
        {
            HookButtons();
        }

        private void OnDisable()
        {
            UnhookButtons();
        }

        public void Bind(ZombieThreatRuntime runtimeThreat, ZombieThreatViewController owner)
        {
            threat = runtimeThreat;
            controller = owner;
            Refresh();
            HookButtons();
        }

        public void Refresh()
        {
            if (threat == null)
            {
                SetText(nameText, string.Empty);
                SetText(healthText, string.Empty);
                SetText(attackText, string.Empty);
                SetArtwork(null);
                SetMarked(false);
                return;
            }

            SetText(nameText, threat.DisplayName);
            SetText(healthText, threat.CurrentHealth.ToString());
            SetText(attackText, threat.Attack.ToString());
            SetArtwork(threat.Artwork);
            SetMarked(threat.IsMarkedForKill);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            SelectThreat();
        }

        private void SelectThreat()
        {
            if (controller != null && threat != null)
            {
                controller.SelectThreat(threat);
            }
        }

        private void ToggleKillMark()
        {
            if (controller != null && threat != null)
            {
                controller.ToggleKillMark(threat);
            }
        }

        private void HookButtons()
        {
            if (healthButton != null)
            {
                healthButton.onClick.RemoveListener(ToggleKillMark);
                healthButton.onClick.AddListener(ToggleKillMark);
            }

            if (selectButton != null)
            {
                selectButton.onClick.RemoveListener(SelectThreat);
                selectButton.onClick.AddListener(SelectThreat);
            }
        }

        private void UnhookButtons()
        {
            if (healthButton != null)
            {
                healthButton.onClick.RemoveListener(ToggleKillMark);
            }

            if (selectButton != null)
            {
                selectButton.onClick.RemoveListener(SelectThreat);
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

        private void SetMarked(bool isMarked)
        {
            if (markedForKillIndicator != null)
            {
                markedForKillIndicator.SetActive(isMarked);
            }
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
