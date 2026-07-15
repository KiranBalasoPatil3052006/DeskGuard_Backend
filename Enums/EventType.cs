/// <summary>
/// Represents the type of event recorded in the audit log.
/// Maps to the 'event_type' column in the audit_logs table.
/// Values match the existing PHP EventType enum exactly.
/// </summary>
namespace DeskGuardBackend.Enums
{
    public enum EventType
    {
        Login,
        Logout,
        Create,
        Update,
        Delete,
        Register,
        Activate,
        Deactivate,
        Alert,
        Resolve,
        Acknowledge,
        Export
    }
}
