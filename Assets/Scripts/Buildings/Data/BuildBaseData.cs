using System.Collections.Generic;
using UnityEngine;
using ZombieCardSurvive.Shared.AutoId;

namespace ZombieCardSurvive.Buildings.Data
{
    [CreateAssetMenu(fileName = "Building", menuName = "Zombie Card Survive/Buildings/Building")]
    public class BuildBaseData : ScriptableObject, IAutoIdAsset
    {
        [SerializeField] private string buildingId;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite artwork;
        [SerializeField] private Color color = Color.white;
        [SerializeField] private Vector2 anchoredPosition;
        [SerializeField] private bool startsUnlocked;
        [SerializeField] private List<string> unlockRequirementIds = new List<string>();
        [SerializeField] private List<BuildingLevelData> levels = new List<BuildingLevelData>();
        [SerializeField] private List<BuildingInteractionData> interactions = new List<BuildingInteractionData>();

        public string BuildingId => buildingId;
        public string DisplayName => displayName;
        public Sprite Artwork => artwork;
        public Color Color => color;
        public Vector2 AnchoredPosition => anchoredPosition;
        public bool StartsUnlocked => startsUnlocked;
        public IReadOnlyList<string> UnlockRequirementIds => unlockRequirementIds;
        public IReadOnlyList<BuildingLevelData> Levels => levels;
        public IReadOnlyList<BuildingInteractionData> Interactions => interactions;
        public string Id => buildingId;

        private void OnValidate()
        {
            EnsureId();
        }

        public void EnsureId()
        {
            if (string.IsNullOrWhiteSpace(buildingId))
            {
                buildingId = AutoIdUtility.CreateId("building");
            }
        }

        public void RegenerateId()
        {
            buildingId = AutoIdUtility.CreateId("building");
        }

        public BuildingLevelData GetLevel(int level)
        {
            if (levels == null)
            {
                return null;
            }

            foreach (BuildingLevelData levelData in levels)
            {
                if (levelData != null && levelData.Level == level)
                {
                    return levelData;
                }
            }

            return null;
        }
    }
}
