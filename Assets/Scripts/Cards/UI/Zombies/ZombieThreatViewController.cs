using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.Cards.UI.Zombies
{
    public class ZombieThreatViewController : MonoBehaviour
    {
        [SerializeField] private Transform zombieRoot;
        [SerializeField] private ZombieThreatView zombieThreatViewPrefab;
        [SerializeField] private ZombieThreatDetailView detailView;

        private readonly Dictionary<int, ZombieThreatView> viewsByThreatId = new Dictionary<int, ZombieThreatView>();
        private readonly List<int> staleViewIds = new List<int>();

        private void Reset()
        {
            zombieRoot = transform;
            detailView = FindObjectOfType<ZombieThreatDetailView>(true);
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

        public void SelectThreat(ZombieThreatRuntime threat)
        {
            if (detailView != null)
            {
                detailView.Open(threat);
            }
        }

        public void ToggleKillMark(ZombieThreatRuntime threat)
        {
            bool changed = CombatSystem.TryToggleZombieKillMark(threat);
            if (!changed)
            {
                Debug.Log("Not enough combat value to mark this zombie for kill.");
            }

            Refresh();
        }

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
    }
}
