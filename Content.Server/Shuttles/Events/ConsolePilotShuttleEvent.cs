namespace Content.Server.Shuttles.Events;

/// <summary>
/// Raised when a shuttle console is trying to control
/// </summary>
/// <param name="Cancelled"></param>

[ByRefEvent]
public record struct ControlShuttleEvent(EntityUid Uid, bool Cancelled, string Reason);
