using UnityEngine;
using UnityEngine.InputSystem;
using ZombieCardSurvive.Run;
using ZombieCardSurvive.Cards.UI.Piles;
using ZombieCardSurvive.Events.Runtime;

public class ShortcutInputController : MonoBehaviour
{
    [SerializeField] private RunPhaseController runPhaseController;
    [SerializeField] private CardPileViewSystem cardPileViewSystem;
    [SerializeField] private EventFlowController eventFlowController;

    private GameInputActions inputActions;
    private InputAction endTurn;
    private InputAction endEvent;
    private InputAction closePopup;

    private void Awake()
    {
        if (runPhaseController == null)
        {
            runPhaseController = FindObjectOfType<RunPhaseController>();
        }

        if (eventFlowController == null)
        {
            eventFlowController = FindObjectOfType<EventFlowController>();
        }

        inputActions = new GameInputActions();
        endTurn = inputActions.GamePlay.EndTurn;
        endEvent = inputActions.GamePlay.EndEvent;
        closePopup = inputActions.GamePlay.ClosePopup;
    }

    private void OnEnable()
    {
        endTurn.performed += OnEndTurn;
        endEvent.performed += OnEndEvent;
        closePopup.performed += OnClosePopup;

        inputActions.GamePlay.Enable();
    }

    private void OnDisable()
    {
        endTurn.performed -= OnEndTurn;
        endEvent.performed -= OnEndEvent;
        closePopup.performed -= OnClosePopup;

        inputActions.GamePlay.Disable();
    }

    private void OnEndTurn(InputAction.CallbackContext context)
    {
        if (runPhaseController != null)
        {
            runPhaseController.EndTurn();
        }
    }

    private void OnEndEvent(InputAction.CallbackContext context)
    {
        if (eventFlowController != null)
        {
            eventFlowController.EndEvent();
            return;
        }

        if (runPhaseController != null)
        {
            runPhaseController.ConfirmEventChoice();
        }
    }


    private void OnClosePopup(InputAction.CallbackContext context)
    {
        if (cardPileViewSystem != null)
        {
            cardPileViewSystem.Close();
        }
    }
}
