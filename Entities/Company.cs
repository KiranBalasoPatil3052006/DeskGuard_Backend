/// <summary>
/// Represents a company (tenant) in the multi-tenant DeskGuard system.
/// Every entity in the system is scoped to a company.
/// Maps to the 'companies' table in PostgreSQL.
/// </summary>
namespace DeskGuardBackend.Entities
{
    public class Company
    {
        /// <summary>Primary key. Auto-incremented.</summary>
        public long Id { get; set; }

        /// <summary>Company display name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Company contact email (unique, nullable).</summary>
        public string? Email { get; set; }

        /// <summary>Company phone number.</summary>
        public string? Phone { get; set; }

        /// <summary>Company physical address.</summary>
        public string? Address { get; set; }

        /// <summary>Company website URL.</summary>
        public string? Website { get; set; }

        /// <summary>Whether the company is active in the system.</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Record creation timestamp (UTC).</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Record last update timestamp (UTC).</summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>FK to the alert profile assigned to this company.</summary>
        public long? AlertProfileId { get; set; }

        // Navigation properties
        /// <summary>Users belonging to this company.</summary>
        public ICollection<User> Users { get; set; } = new List<User>();

        /// <summary>Machines registered under this company.</summary>
        public ICollection<Machine> Machines { get; set; } = new List<Machine>();

        /// <summary>Alerts generated for this company.</summary>
        public ICollection<Alert> Alerts { get; set; } = new List<Alert>();

        /// <summary>Alert profile assigned to this company.</summary>
        public AlertProfile? AlertProfile { get; set; }
    }
}
