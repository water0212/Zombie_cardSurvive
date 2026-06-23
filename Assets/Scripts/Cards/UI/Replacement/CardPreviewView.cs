using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZombieCardSurvive.Cards.Data;
using ZombieCardSurvive.Cards.Runtime;

namespace ZombieCardSurvive.Cards.UI.Replacement
{
    public class CardPreviewView : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text energyText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text remainingUsesText;
        [SerializeField] private Image artworkImage;
        [SerializeField] private Image backgroundImage;

        public void Bind(CardRuntime card)
        {
            Bind(card != null ? card.Data : null);

            if (remainingUsesText == null)
            {
                return;
            }

            if (card != null && card.Data != null && card.Data.HasLimitedUses)
            {
                remainingUsesText.text = card.RemainingUses.ToString();
                remainingUsesText.gameObject.SetActive(true);
            }
            else
            {
                remainingUsesText.gameObject.SetActive(false);
            }
        }

        public void Bind(CardBase card)
        {
            if (card == null)
            {
                SetText(nameText, string.Empty);
                SetText(energyText, string.Empty);
                SetText(descriptionText, string.Empty);
                SetArtwork(null);
                SetBackground(null);
                SetRemainingUses(false, string.Empty);
                return;
            }

            SetText(nameText, card.DisplayName);
            SetText(energyText, card.EnergyCost.ToString());
            SetText(descriptionText, card.Description);
            SetArtwork(card.Artwork);
            SetBackground(card.Background);

            if (card.HasLimitedUses)
            {
                SetRemainingUses(true, card.MaxUsesPerRun.ToString());
            }
            else
            {
                SetRemainingUses(false, string.Empty);
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

        private void SetBackground(Sprite sprite)
        {
            if (backgroundImage == null)
            {
                return;
            }

            backgroundImage.sprite = sprite;
            backgroundImage.enabled = sprite != null;
        }

        private void SetRemainingUses(bool isVisible, string value)
        {
            if (remainingUsesText == null)
            {
                return;
            }

            remainingUsesText.text = value;
            remainingUsesText.gameObject.SetActive(isVisible);
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
