using UnityEngine;
using ZombieCardSurvive.Shared.AutoId;

namespace ZombieCardSurvive.Endings.Data
{
    [CreateAssetMenu(fileName = "EndingKeyItem", menuName = "Zombie Card Survive/Endings/Key Item")]
    public sealed class EndingKeyItemData : ScriptableObject, IAutoIdAsset
    {
        [SerializeField] private string keyItemId;
        [SerializeField] private string displayName;
        [TextArea]
        [SerializeField] private string description;
        [SerializeField] private Sprite artwork;
        [SerializeField] private bool hiddenUntilDiscovered = true;

        public string KeyItemId => keyItemId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Artwork => artwork;
        public bool HiddenUntilDiscovered => hiddenUntilDiscovered;
        public string Id => keyItemId;

        private void OnValidate()
        {
            EnsureId();
        }

        public void EnsureId()
        {
            if (string.IsNullOrWhiteSpace(keyItemId))
            {
                keyItemId = AutoIdUtility.CreateId("ending_key_item");
            }
        }

        public void RegenerateId()
        {
            keyItemId = AutoIdUtility.CreateId("ending_key_item");
        }
    }
}
