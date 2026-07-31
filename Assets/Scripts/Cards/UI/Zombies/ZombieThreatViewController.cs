using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Cards.UI.Zombies
{
    /// <summary>
    /// 殭屍威脅列的 UI 控制器。
    /// 負責依 CombatSystem 目前的 ZombieThreatRuntime 建立、刷新、移除畫面上的殭屍物件。
    /// </summary>
    public class ZombieThreatViewController : MonoBehaviour
    {
        // 殭屍 UI 實例要掛載的父節點。
        [SerializeField] private Transform zombieRoot;
        // 單一殭屍威脅的 UI Prefab。
        [SerializeField] private ZombieThreatView zombieThreatViewPrefab;
        // 點選殭屍後開啟的詳細資訊面板。
        [SerializeField] private ZombieThreatDetailView detailView;

        // RuntimeId -> UI View 的對照表，用來重用既有殭屍 UI。
        private readonly Dictionary<int, ZombieThreatView> viewsByThreatId = new Dictionary<int, ZombieThreatView>();
        // 暫存這次刷新後已不存在的殭屍 ID，避免邊走訪 Dictionary 邊刪除。
        private readonly List<int> staleViewIds = new List<int>();

        private void Reset()
        {
            zombieRoot = transform;
            detailView = FindObjectOfType<ZombieThreatDetailView>(true);
        }

        private void Awake()
        {
            ResolveDetailView();
        }

        private void OnEnable()
        {
            CombatSystem.StateChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            CombatSystem.StateChanged -= Refresh;
        }

        /// <summary>
        /// 依 CombatSystem 的目前狀態同步殭屍 UI：新增缺少的 View，刪除已消失的 View。
        /// </summary>
        [ContextMenu("Refresh")]
        public void Refresh()
        {
            if (zombieRoot == null)
            {
                zombieRoot = transform;
            }

            staleViewIds.Clear();
            foreach (int id in viewsByThreatId.Keys)
            {
                staleViewIds.Add(id);
            }

            foreach (ZombieThreatRuntime threat in CombatSystem.ZombieThreats)
            {
                if (threat == null)
                {
                    continue;
                }

                staleViewIds.Remove(threat.RuntimeId);
                ZombieThreatView view = GetOrCreateView(threat);
                if (view != null)
                {
                    view.Bind(threat, this);
                }
            }

            for (int i = 0; i < staleViewIds.Count; i++)
            {
                RemoveView(staleViewIds[i]);
            }
        }

        /// <summary>
        /// 選取一個殭屍威脅並開啟詳細面板。
        /// </summary>
        /// <param name="threat">被玩家點選的殭屍 Runtime。</param>
        public void SelectThreat(ZombieThreatRuntime threat)
        {
            if (ResolveDetailView())
            {
                detailView.Open(threat);
            }
            else
            {
                Debug.LogWarning("ZombieThreatDetailView was not found. The selected zombie cannot be displayed.", this);
            }
        }

        /// <summary>
        /// 切換殭屍的擊殺標記；CombatSystem 會檢查目前戰鬥力是否足夠。
        /// </summary>
        /// <param name="threat">要標記或取消標記的殭屍 Runtime。</param>
        public void ToggleKillMark(ZombieThreatRuntime threat)
        {
            bool changed = CombatSystem.TryToggleZombieKillMark(threat);
            if (!changed)
            {
                Debug.Log("Not enough combat value to mark this zombie for kill.");
            }

            Refresh();
        }

        // 取得既有殭屍 UI；沒有時才從 Prefab 建立新 View。
        private ZombieThreatView GetOrCreateView(ZombieThreatRuntime threat)
        {
            if (viewsByThreatId.TryGetValue(threat.RuntimeId, out ZombieThreatView existingView))
            {
                return existingView;
            }

            if (zombieThreatViewPrefab == null)
            {
                Debug.LogWarning("ZombieThreatView prefab is not assigned.");
                return null;
            }

            ZombieThreatView view = Instantiate(zombieThreatViewPrefab, zombieRoot);
            view.gameObject.SetActive(true);
            viewsByThreatId.Add(threat.RuntimeId, view);
            return view;
        }

        // 移除已經不存在的殭屍威脅 UI。
        private void RemoveView(int threatId)
        {
            if (!viewsByThreatId.TryGetValue(threatId, out ZombieThreatView view))
            {
                return;
            }

            viewsByThreatId.Remove(threatId);
            if (view != null)
            {
                Destroy(view.gameObject);
            }
        }

        // 嘗試取得詳細面板；即使一開始沒開啟，也會包含 inactive 物件搜尋。
        private bool ResolveDetailView()
        {
            if (detailView == null)
            {
                detailView = FindObjectOfType<ZombieThreatDetailView>(true);
            }

            return detailView != null;
        }
    }
}
