using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZombieCardSurvive.Run;
using ZombieCardSurvive.Systems.Transactions;

namespace ZombieCardSurvive.UI.Settlement
{
    public sealed class CollapseReportView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text reportText;
        [SerializeField] private Button restartButton, leaveButton;
        private bool requestSent;
        public event Action RestartRequested;
        public event Action LeaveRequested;
        public bool IsVisible => panel != null && panel.activeSelf;
        public void Configure(GameObject root, TMP_Text text, Button restart, Button leave)
        { panel = root; reportText = text; restartButton = restart; leaveButton = leave; Hide(); }
        public void Show(ValueTransactionResult result)
        {
            requestSent = false;
            if (reportText != null) reportText.text = SettlementResultFormatter.CollapseSummary(result);
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(RequestRestart); restartButton.onClick.AddListener(RequestRestart);
                restartButton.gameObject.SetActive(RestartRequested != null);
            }
            if (leaveButton != null)
            {
                leaveButton.onClick.RemoveListener(RequestLeave); leaveButton.onClick.AddListener(RequestLeave);
                leaveButton.gameObject.SetActive(LeaveRequested != null);
            }
            if (panel != null) panel.SetActive(true);
        }
        public void Hide() { requestSent = false; if (panel != null) panel.SetActive(false); }
        public void RequestRestart() { if (requestSent || RestartRequested == null) return; requestSent = true; RestartRequested.Invoke(); }
        public void RequestLeave() { if (requestSent || LeaveRequested == null) return; requestSent = true; LeaveRequested.Invoke(); }
    }
}
