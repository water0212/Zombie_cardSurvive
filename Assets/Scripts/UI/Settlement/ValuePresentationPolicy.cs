using ZombieCardSurvive.Systems.Transactions;

namespace ZombieCardSurvive.UI.Settlement
{
    public static class ValuePresentationPolicy
    {
        // Failure keeps its existing immediate gameplay guard and report handoff.
        public static bool RequiresBlocking(ValueTransactionResult result)
            => result != null && result.Kind != ValueTransactionKind.Reset && (result.Kind == ValueTransactionKind.RoundEnd
                || result.Morale?.ChangeResult.StageChanged == true || result.IsCollapsed);
    }
}
