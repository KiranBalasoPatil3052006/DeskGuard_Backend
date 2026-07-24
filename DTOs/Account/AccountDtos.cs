using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DeskGuardBackend.DTOs.Account
{
    public class CreateAccountRequest
    {
        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("mobile_number")]
        public string? MobileNumber { get; set; }

        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;

        [JsonPropertyName("confirm_password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [JsonPropertyName("employee_id")]
        public string EmployeeId { get; set; } = string.Empty;

        [JsonPropertyName("role")]
        public string Role { get; set; } = "Admin";

        [JsonPropertyName("status")]
        public string Status { get; set; } = "active";
    }

    public class UpdateAccountRequest
    {
        [JsonPropertyName("full_name")]
        public string? FullName { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("mobile_number")]
        public string? MobileNumber { get; set; }

        [JsonPropertyName("employee_id")]
        public string? EmployeeId { get; set; }

        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }

    public class ResetPasswordRequest
    {
        [JsonPropertyName("new_password")]
        public string NewPassword { get; set; } = string.Empty;

        [JsonPropertyName("confirm_password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [JsonPropertyName("must_change_password")]
        public bool MustChangePassword { get; set; } = true;
    }

    public class AccountDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("employee_id")]
        public string? EmployeeId { get; set; }

        [JsonPropertyName("full_name")]
        public string? FullName { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("mobile_number")]
        public string? MobileNumber { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = "active";

        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; } = true;

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("last_login")]
        public DateTime? LastLogin { get; set; }

        [JsonPropertyName("created_by")]
        public string? CreatedBy { get; set; }
    }

    public class AccountListResponse
    {
        [JsonPropertyName("data")]
        public List<AccountDto> Data { get; set; } = new();

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("per_page")]
        public int PerPage { get; set; }

        [JsonPropertyName("total_pages")]
        public int TotalPages { get; set; }
    }

    public class AccountFilterRequest
    {
        [JsonPropertyName("search")]
        public string? Search { get; set; }

        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("sort_by")]
        public string? SortBy { get; set; } = "created_at";

        [JsonPropertyName("sort_order")]
        public string? SortOrder { get; set; } = "desc";

        [JsonPropertyName("page")]
        public int Page { get; set; } = 1;

        [JsonPropertyName("per_page")]
        public int PerPage { get; set; } = 20;
    }
}
