using UnityEngine;
using ZombieCardSurvive.Shared.AutoId;

namespace ZombieCardSurvive.Buildings.Data
{
    public abstract class BuildingInteractionData : ScriptableObject, IAutoIdAsset
    {
        [SerializeField] private string interactionId;
        [SerializeField] private string displayName;
        [Min(1)]
        [SerializeField] private int requiredBuildingLevel = 1;

        public string InteractionId => interactionId;
        public string DisplayName => displayName;
        public int RequiredBuildingLevel => Mathf.Max(1, requiredBuildingLevel);
        public string Id => interactionId;

        protected virtual void OnValidate()
        {
            EnsureId();
        }

        public void EnsureId()
        {
            if (string.IsNullOrWhiteSpace(interactionId))
            {
                interactionId = AutoIdUtility.CreateId("building_interaction");
            }
        }

        public void RegenerateId()
        {
            interactionId = AutoIdUtility.CreateId("building_interaction");
        }
    }
}
