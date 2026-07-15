/// <summary>
/// Represents the lifecycle status of an alert.
/// Maps to the 'status' column in the alerts table.
/// Values match the existing PHP AlertStatus enum exactly.
/// </summary>
namespace DeskGuardBackend.Enums
{
    public enum AlertStatus
    {
        Open,
        Acknowledged,
        Resolved,
        Dismissed,
        Escalated
    }
}
