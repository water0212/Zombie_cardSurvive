namespace ZombieCardSurvive.Shared.AutoId
{
    public interface IAutoIdAsset
    {
        string Id { get; }
        void EnsureId();
        void RegenerateId();
    }
}
