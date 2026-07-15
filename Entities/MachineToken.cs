/// <summary>
/// Stores machine authentication tokens for agent API access.
/// The token is SHA-256 hashed before storage for security.
/// Maps to the 'machine_tokens' table in PostgreSQL.
/// </summary>
namespace DeskGuardBackend.Entities
{
    public class MachineToken
    {
        public long Id { get; set; }
        public long MachineId { get; set; }

        /// <summary>SHA-256 hash of the bearer token.</summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>Token expiration date (null = never expires).</summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>Last time this token was used for an API call.</summary>
        public DateTime? LastUsedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Machine Machine { get; set; } = null!;
    }
}
