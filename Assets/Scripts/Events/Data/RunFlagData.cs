using UnityEngine;
using ZombieCardSurvive.Shared.AutoId;

namespace ZombieCardSurvive.Events.Data
{
    [CreateAssetMenu(fileName = "RunFlagData", menuName = "Zombie Card Survive/Events/Run Flag")]
    public sealed class RunFlagData : ScriptableObject, IAutoIdAsset
    {
        [SerializeField] private string flagId;
        [SerializeField] private string displayName;
        [TextArea]
        [SerializeField] private string description;
        [SerializeField] private int defaultValue;
        [SerializeField] private int minimumValue;
        [Tooltip("0 means this flag has no maximum value.")]
        [Min(0)]
        [SerializeField] private int maximumValue;

        public string FlagId => flagId;
        public string DisplayName => displayName;
        public string Description => description;
        public int DefaultValue => Clamp(defaultValue);
        public int MinimumValue => minimumValue;
        public int MaximumValue => Mathf.Max(0, maximumValue);
        public string Id => flagId;

        private void OnValidate()
        {
            EnsureId();
            if (maximumValue > 0 && minimumValue > maximumValue)
            {
                minimumValue = maximumValue;
            }

            defaultValue = Clamp(defaultValue);
        }

        public void EnsureId()
        {
            if (string.IsNullOrWhiteSpace(flagId))
            {
                flagId = AutoIdUtility.CreateId("flag");
            }
        }

        public void RegenerateId()
        {
            flagId = AutoIdUtility.CreateId("flag");
        }

        public int Clamp(int value)
        {
            int clamped = Mathf.Max(minimumValue, value);
            return maximumValue > 0 ? Mathf.Min(maximumValue, clamped) : clamped;
        }
    }
}
