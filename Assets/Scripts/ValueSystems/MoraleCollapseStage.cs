namespace ZombieCardSurvive.Systems
{
    /// <summary>
    /// 本局士氣曾抵達的最嚴重崩潰階段。階段只能往下惡化，不會因士氣恢復而倒退。
    /// </summary>
    public enum MoraleCollapseStage
    {
        Stable = 0,
        Shaken = 1,
        Brink = 2
    }
}
