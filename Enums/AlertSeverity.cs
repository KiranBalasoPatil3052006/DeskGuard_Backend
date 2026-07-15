/// <summary>
/// Represents the severity level of an alert in the DeskGuard system.
/// Maps to the 'severity' column in the alerts table.
/// Values match the existing PHP AlertSeverity enum exactly.
/// </summary>
namespace DeskGuardBackend.Enums
{
    public enum AlertSeverity
    {
        Info,
        Low,
        Medium,
        High,
        Critical
    }
}
