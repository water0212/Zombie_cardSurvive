using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Systems;

namespace ZombieCardSurvive.UI.World
{
    /// <summary>以固定 3×4 右側格位呈現屍群；不再依數量重新置中。</summary>
    public sealed class ZombieThreatSpriteViewController : MonoBehaviour
    {
        [SerializeField] private ZombieThreatSpriteView viewPrefab;
        [SerializeField] private List<Transform> slots = new List<Transform>();
        [SerializeField] private Transform poolRoot;
        private readonly List<ZombieThreatSpriteView> views = new List<ZombieThreatSpriteView>();

        public void Configure(ZombieThreatSpriteView prefab, List<Transform> slotList, Transform pool)
        {
            viewPrefab = prefab;
            slots = slotList ?? new List<Transform>();
            poolRoot = pool;
        }

        private void OnEnable()
        {
            CombatSystem.StateChanged -= Refresh;
            CombatSystem.StateChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            CombatSystem.StateChanged -= Refresh;
        }

        public void Refresh()
        {
            int visible = Mathf.Min(slots.Count, CombatSystem.ZombieThreats.Count);
            EnsureViews(visible);

            for (int index = 0; index < views.Count; index++)
            {
                bool active = index < visible;
                views[index].gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                Transform slot = slots[index];
                Transform viewTransform = views[index].transform;
                viewTransform.SetParent(slot, false);
                viewTransform.localPosition = Vector3.zero;
                viewTransform.localRotation = Quaternion.identity;
                views[index].Bind(CombatSystem.ZombieThreats[index]);
            }
        }

        private void EnsureViews(int count)
        {
            if (viewPrefab == null)
            {
                return;
            }

            while (views.Count < count)
            {
                ZombieThreatSpriteView view = Instantiate(viewPrefab, poolRoot != null ? poolRoot : transform);
                view.name = $"ZombieThreatSpriteView_{views.Count + 1:00}";
                views.Add(view);
            }
        }
    }
}
