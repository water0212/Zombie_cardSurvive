using UnityEngine;
using ZombieCardSurvive.UI.Settlement;

namespace ZombieCardSurvive.Run
{
    [DefaultExecutionOrder(-10000)]
    public sealed class RunSceneEntry : MonoBehaviour
    {
        private CollapseReportView report;
        private void Awake() { RunLifecycleController.Ensure(); }
        private void Start()
        {
            report = FindObjectOfType<CollapseReportView>(true);
            if (report != null) report.RestartRequested += ReturnToMenu;
            RunLifecycleController.Instance.StartDirectGameplay(FindObjectOfType<RunPhaseController>());
        }
        private void ReturnToMenu() { RunLifecycleController.Instance?.RequestReturnToMenu(); }
        private void OnDestroy()
        {
            if (report != null) report.RestartRequested -= ReturnToMenu;
        }
    }
}
