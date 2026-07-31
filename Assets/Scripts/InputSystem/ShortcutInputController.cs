using UnityEngine;
using UnityEngine.InputSystem;
using ZombieCardSurvive.Run;
using ZombieCardSurvive.Cards.UI.Piles;
using ZombieCardSurvive.Events.Runtime;

/// <summary>
/// 全域快捷鍵控制器。
/// 負責把 Input System 的按鍵事件轉成遊戲操作，例如結束回合、結束事件與關閉牌堆視窗。
/// </summary>
public class ShortcutInputController : MonoBehaviour
{
    // 接收「結束回合」與部分事件確認的流程控制器。
    [SerializeField] private RunPhaseController runPhaseController;
    // 接收「關閉彈窗」操作的牌堆檢視系統。
    [SerializeField] private CardPileViewSystem cardPileViewSystem;
    // 若場景有事件流程控制器，EndEvent 快捷鍵會優先交給它。
    [SerializeField] private EventFlowController eventFlowController;

    // Input System 產生的輸入設定包。
    private GameInputActions inputActions;
    // 結束目前 Turn 的輸入動作。
    private InputAction endTurn;
    // 確認或關閉事件流程的輸入動作。
    private InputAction endEvent;
    // 關閉目前牌堆或彈窗的輸入動作。
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

    // EndTurn 快捷鍵：只負責轉送給 RunPhaseController，由階段控制器判斷是否能結束。
    private void OnEndTurn(InputAction.CallbackContext context)
    {
        if (runPhaseController != null)
        {
            runPhaseController.EndTurn();
        }
    }

    // EndEvent 快捷鍵：有事件流程時優先關事件，否則退回 RunPhaseController 的確認事件入口。
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

    // ClosePopup 快捷鍵：目前用於關閉牌堆檢視視窗。
    private void OnClosePopup(InputAction.CallbackContext context)
    {
        if (cardPileViewSystem != null)
        {
            cardPileViewSystem.Close();
        }
    }
}
