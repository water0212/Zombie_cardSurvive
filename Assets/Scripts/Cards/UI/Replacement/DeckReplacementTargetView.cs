using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZombieCardSurvive.Cards.Runtime;

namespace ZombieCardSurvive.Cards.UI.Replacement
{
    public class DeckReplacementTargetView : MonoBehaviour, IDropHandler
    {
        [SerializeField] private CardPreviewView previewView;
        [SerializeField] private Image dimmingOverlay;
        [SerializeField] private GameObject highlightRoot;

        private DeckReplacementView owner;

        public CardRuntime RuntimeCard { get; private set; }

        public void Bind(CardRuntime card, DeckReplacementView replacementView)
        {
            RuntimeCard = card;
            owner = replacementView;

            if (previewView != null)
            {
                previewView.Bind(card);
            }

            SetAvailability(false);
        }

        public void SetAvailability(bool isAvailable)
        {
            if (dimmingOverlay != null)
            {
                dimmingOverlay.gameObject.SetActive(!isAvailable);
            }

            if (highlightRoot != null)
            {
                highlightRoot.SetActive(isAvailable);
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (owner == null)
            {
                return;
            }

            DeckReplacementCandidateView candidateView = eventData.pointerDrag != null
                ? eventData.pointerDrag.GetComponent<DeckReplacementCandidateView>()
                : null;

            if (candidateView != null)
            {
                owner.TryReplace(RuntimeCard, candidateView.Entry);
            }
        }
    }
}
