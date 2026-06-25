using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Cards.UI.Zombies
{
    public class ZombieThreatDetailView : MonoBehaviour
    {
        private GameObject root;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private TMP_Text attackText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text negativeEffectText;
        [SerializeField] private Image artworkImage;
        [SerializeField] private Button closeButton;

        private void Awake()
        {
            if (root == null)
            {
                root = gameObject;
            }
        }

        private void OnEnable()
        {
            HookButtons();
        }

        private void OnDisable()
        {
            UnhookButtons();
        }

        public void Open(ZombieThreatRuntime threat)
        {
            if (threat == null)
            {
                Close();
                return;
            }

            SetText(nameText, threat.DisplayName);
            SetText(healthText, $"{threat.CurrentHealth}/{threat.MaxHealth}");
            SetText(attackText, threat.Attack.ToString());
            SetText(descriptionText, threat.Description);
            SetText(negativeEffectText, threat.NegativeEffectDescription);
            SetArtwork(threat.Artwork);

            if (root != null)
            {
                root.SetActive(true);
            }
        }

        public void Close()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        private void HookButtons()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
                closeButton.onClick.AddListener(Close);
            }
        }

        private void UnhookButtons()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
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
