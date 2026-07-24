using System;
using System.Collections.Generic;

namespace DeskGuardBackend.Entities
{
    /// <summary>
    /// Represents an AMC Customer entity grouping machines by Company Name and Mobile Number.
    /// Maps to the 'customers' table in PostgreSQL.
    /// </summary>
    public class Customer
    {
        public long Id { get; set; }

        /// <summary>Unique customer code (e.g. CUST-1001).</summary>
        public string CustomerCode { get; set; } = string.Empty;

        /// <summary>Company Name.</summary>
        public string CompanyName { get; set; } = string.Empty;

        /// <summary>Customer Name (Primary Contact Person).</summary>
        public string CustomerName { get; set; } = string.Empty;

        /// <summary>Primary contact Mobile Number.</summary>
        public string MobileNumber { get; set; } = string.Empty;

        /// <summary>Primary contact Email Address.</summary>
        public string? Email { get; set; }

        /// <summary>Account Status (e.g., Active, Suspended, Archived).</summary>
        public string Status { get; set; } = "Active";

        /// <summary>Optional remarks or notes for IT technicians.</summary>
        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Collection of machines belonging to this customer.</summary>
        public ICollection<Machine> Machines { get; set; } = new List<Machine>();
    }
}
