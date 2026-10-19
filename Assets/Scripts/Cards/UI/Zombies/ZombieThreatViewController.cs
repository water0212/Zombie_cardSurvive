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
        // 每一排保留的殭屍空位數。第一隻在中心，後續由中心向左右補位。
        [SerializeField, Min(1)] private int slotsPerRow = 7;
        // 第一個空位的中心位置；維持為 0 時，第一隻殭屍會留在 ZombieThreatRoot 中心。
        [SerializeField] private Vector2 layoutOrigin = Vector2.zero;
        // 空位間距。Y 通常為負值，讓超過第一排的殭屍往下排。
        [SerializeField] private Vector2 slotSpacing = new Vector2(190f, -210f);
        // 第二排之後的縮放，避免屍潮數量變多時遮住主要畫面。
        [SerializeField, Min(0.1f)] private float overflowRowScale = 0.85f;

        // RuntimeId -> UI View 的對照表，用來重用既有殭屍 UI。
        private readonly Dictionary<int, ZombieThreatView> viewsByThreatId = new Dictionary<int, ZombieThreatView>();
        // 依 CombatSystem 順序收集本次可見 View，用於套用穩定排版。
        private readonly List<ZombieThreatView> orderedViews = new List<ZombieThreatView>();
        // 暫存這次刷新後已不存在的殭屍 ID，避免邊走訪 Dictionary 邊刪除。
        private readonly List<int> staleViewIds = new List<int>();

        private void Reset()
        {
            zombieRoot = transform;
            detailView = FindObjectOfType<ZombieThreatDetailView>(true);
            NormalizeLayoutSettings();
        }

        private void OnValidate()
        {
            NormalizeLayoutSettings();
        }

        private void Awake()
        {
            NormalizeLayoutSettings();
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

            orderedViews.Clear();
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
                    orderedViews.Add(view);
                }
            }

            for (int i = 0; i < staleViewIds.Count; i++)
            {
                RemoveView(staleViewIds[i]);
            }

            ApplyLayout();
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

        // 套用中心向外的固定空位排版，避免多隻殭屍生成在同一個位置。
        private void ApplyLayout()
        {
            for (int i = 0; i < orderedViews.Count; i++)
            {
                ZombieThreatView view = orderedViews[i];
                if (view == null)
                {
                    continue;
                }

                ZombieThreatLayoutSlot slot = ZombieThreatLayoutCalculator.CalculateSlot(
                    i,
                    slotsPerRow,
                    layoutOrigin,
                    slotSpacing,
                    overflowRowScale);

                ApplySlot(view.transform, slot);
                view.transform.SetSiblingIndex(i);
            }
        }

        private static void ApplySlot(Transform viewTransform, ZombieThreatLayoutSlot slot)
        {
            if (viewTransform == null)
            {
                return;
            }

            if (viewTransform is RectTransform rectTransform)
            {
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = slot.AnchoredPosition;
            }
            else
            {
                viewTransform.localPosition = new Vector3(slot.AnchoredPosition.x, slot.AnchoredPosition.y, 0f);
            }

            viewTransform.localScale = Vector3.one * slot.Scale;
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

        private void NormalizeLayoutSettings()
        {
            slotsPerRow = Mathf.Max(1, slotsPerRow);

            if (slotSpacing == Vector2.zero)
            {
                slotSpacing = new Vector2(190f, -210f);
            }

            overflowRowScale = Mathf.Max(0.1f, overflowRowScale);
        }
    }
}
