namespace HY3RIDOrigins.Squad
{
    /// Commands issued to squad members via the radial command wheel.
    /// Phase 5 will connect SquadCommandSystem as a subscriber to CommandWheelUI.OnCommandSelected.
    public enum SquadCommandType { Follow, Hold, Focus, Aggressive, Defensive }
}
