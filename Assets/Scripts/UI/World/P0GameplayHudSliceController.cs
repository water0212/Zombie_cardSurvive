using ZombieCardSurvive.Systems.Transactions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZombieCardSurvive.Cards.Data;
using ZombieCardSurvive.Cards.Runtime;
using ZombieCardSurvive.Run;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.UI.World
{
    /// <summary>只服務 P0 Integration Scene 的可操作驗證資料。</summary>
    public sealed class P0GameplayHudSliceController : MonoBehaviour
    {
        [SerializeField] private ZombieCardData demoZombie;
        [SerializeField] private CardController cardController;
        [SerializeField] private RunPhaseController runPhaseController;
        [SerializeField] private WorldInteractionRouter inputRouter;
        [SerializeField] private TMP_Text lockStateText;
        [SerializeField] private Button lockToggleButton;
        [SerializeField] private ZombieThreatSpriteView demoZombieView;
        [SerializeField] private WorldLabelController worldLabelController;
        [SerializeField] private RectTransform zombieLabel;
        private bool locked;

        public void Configure(
            ZombieCardData zombie,
            CardController cards,
            RunPhaseController phaseController,
            WorldInteractionRouter router,
            TMP_Text lockText,
            Button lockButton,
            ZombieThreatSpriteView zombieView,
            WorldLabelController labelController,
            RectTransform label)
        {
            demoZombie = zombie;
            cardController = cards;
            runPhaseController = phaseController;
            inputRouter = router;
            lockStateText = lockText;
            lockToggleButton = lockButton;
            demoZombieView = zombieView;
            worldLabelController = labelController;
            zombieLabel = label;
        }

        private void OnEnable()
        {
            CombatSystem.StateChanged -= Refresh;
            CombatSystem.StateChanged += Refresh;
            if (lockToggleButton != null)
            {
                lockToggleButton.onClick.RemoveListener(ToggleInputLock);
                lockToggleButton.onClick.AddListener(ToggleInputLock);
            }
        }

        private void OnDisable()
        {
            CombatSystem.StateChanged -= Refresh;
            if (lockToggleButton != null)
            {
                lockToggleButton.onClick.RemoveListener(ToggleInputLock);
            }

            locked = false;
            inputRouter?.SetInputLocked(false);
        }

        private void Start()
        {
            if (runPhaseController != null)
            {
                runPhaseController.StartRun();
                if (!runPhaseController.IsRunEnded)
                {
                    runPhaseController.ConfirmEventChoice();
                }
            }
            else if (cardController != null)
            {
                MoralePyramidSystem.Reset();
                MoraleSystem.Reset();
                cardController.StartDemoRun();
                cardController.StartTurn();
            }
            else
            {
                MoralePyramidSystem.Reset();
                MoraleSystem.Reset();
                FoodSystem.Reset(23);
                ResourceSystem.Reset(17);
                EnergySystem.Reset(5);
                CombatSystem.Reset(9, 50);
            }

            // Demo setup is initialization, never a gameplay gain or stage-transition animation.
            ValueTransactions.ResetPresentation();
            using (var transaction = new ValueTransaction("P0DemoInitialization", ValueTransactionKind.Reset))
            {
                transaction.Execute(() =>
                {
            FoodSystem.SetStorageCapacity(30);
            EnergySystem.SpendEnergy(2);
            MoraleSystem.ReduceMorale(38);
            CombatSystem.AddPendingDamage(6);
            if (demoZombie != null)
            {
                ZombieThreatRuntime threat = CombatSystem.SpawnZombieThreat(demoZombie);
                if (demoZombieView != null)
                {
                    demoZombieView.Bind(threat);
                    worldLabelController?.Bind(threat.RuntimeId, demoZombieView.transform, zombieLabel, new Vector3(0f, 1.15f, 0f));
                }
            }
            Refresh();
                });
                transaction.Complete();
            }
        }

        public void ToggleInputLock()
        {
            locked = !locked;
            inputRouter?.SetInputLocked(locked);
            Refresh();
        }

        [ContextMenu("QA/Set Food Shortage Warning")]
        public void SetFoodShortageWarning()
        {
            if (!RequirePlayMode()) return;
            FoodSystem.Reset(0);
            Debug.Log("QA state applied: permanent food is 0; the HUD should show this round's food shortage warning.", this);
        }

        [ContextMenu("QA/Set Stage Crossing Risk")]
        public void SetStageCrossingRisk()
        {
            if (!RequirePlayMode()) return;
            MoraleSystem.Reset(startingMorale: 72);
            FoodSystem.Reset(0);
            Debug.Log("QA state applied: ending the round should request confirmation for a morale stage crossing.", this);
        }

        [ContextMenu("QA/Restore Demo Values")]
        public void RestoreDemoValues()
        {
            if (!RequirePlayMode()) return;
            FoodSystem.Reset(23);
            MoraleSystem.Reset();
            MoraleSystem.ReduceMorale(38);
            Debug.Log("QA state restored to the P0 demo values.", this);
        }

        private bool RequirePlayMode()
        {
            if (Application.isPlaying) return true;
            Debug.LogWarning("This QA action is available only in Play Mode.", this);
            return false;
        }

        private void Refresh()
        {
            if (lockStateText != null)
            {
                lockStateText.text = locked ? "事件遮罩：世界操作已鎖定" : "世界操作：可用";
                lockStateText.color = locked ? new Color32(158, 43, 43, 255) : new Color32(39, 96, 58, 255);
            }
            demoZombieView?.Refresh();
        }
    }
}
