using ZombieCardSurvive.Events.Data;

namespace ZombieCardSurvive.Events.Runtime
{
    /// <summary>
    /// A future ending runtime can take over the daily event flow without entering the general event pool.
    /// </summary>
    public interface IEventSequenceSource
    {
        bool IsActive { get; }
        bool TryGetNextEvent(int day, out EventData eventData);
    }
}
