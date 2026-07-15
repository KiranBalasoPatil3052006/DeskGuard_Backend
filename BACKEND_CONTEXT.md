# DeskGuard Backend - Backend Context

## Account Management Module

### Overview
The Account Management module allows Super Admin users to create, view, edit, disable, enable, and delete administrator accounts. It extends the existing `User` entity with `EmployeeId` and `CreatedByUserId` fields.

### Entity Changes
- **User.cs**: Added `EmployeeId` (string, unique, EMP-XXXX format), `CreatedByUserId` (long?, FK to self), `CreatedBy` (navigation)

### New Files
| File | Path | Purpose |
|------|------|---------|
| AccountDtos.cs | `DTOs/Account/` | CreateAccountRequest, UpdateAccountRequest, AccountDto, AccountListResponse, AccountFilterRequest |
| IAccountService.cs | `Services/Interfaces/` | Account service interface |
| AccountService.cs | `Services/` | Account service implementation |
| AccountsController.cs | `Controllers/` | REST API endpoints for account management |

### API Endpoints
| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| POST | `/api/v1/accounts` | Create a new admin account | Super Admin |
| GET | `/api/v1/accounts` | List accounts (paginated, searchable, filterable) | Super Admin |
| GET | `/api/v1/accounts/{id}` | Get single account details | Super Admin |
| PUT | `/api/v1/accounts/{id}` | Update account name/email/employee ID | Super Admin |
| DELETE | `/api/v1/accounts/{id}` | Soft-delete an account | Super Admin |
| PATCH | `/api/v1/accounts/{id}/disable` | Disable an account | Super Admin |
| PATCH | `/api/v1/accounts/{id}/enable` | Enable an account | Super Admin |
| GET | `/api/v1/accounts/employee-id/next` | Generate next EMP-XXXX ID | Super Admin |

### Business Logic
- **Create**: Validates required fields, email format, password length (6+), password confirmation match, checks duplicate email/employee ID, hashes password with BCrypt, assigns "Admin" role
- **List**: Filters by search (name/email/employee ID), status (active/disabled/all), paginated
- **Update**: Only updates provided fields, re-validates uniqueness
- **Delete**: Soft delete (sets `DeletedAt` timestamp)
- **Disable/Enable**: Toggles `IsActive` flag
- **Employee ID**: Auto-generates EMP-0001, EMP-0002, etc., with duplicate collision check

### Role System
- **Super Admin** (Role ID 1): Full access — create, read, update, delete, disable, enable accounts
- **Admin** (Role ID 4): Limited access — cannot manage accounts
- Authorization enforced at controller level via JWT claim check

### Exception
- `AccountException` — thrown for validation failures (422) and not-found (404)

---

## Alert Threshold Management Module

### Overview
The Alert Threshold Management module introduces profile-based alert threshold configuration. Administrators create named profiles with categorized thresholds (Performance, Storage, Availability, Authentication, Network) and assign them to companies or individual machines.

### Entity Changes
- **Company.cs**: Added `AlertProfileId` (long?, FK → alert_profiles), `AlertProfile` (navigation)
- **Machine.cs**: Added `CustomAlertProfileId` (long?, FK → alert_profiles), `CustomAlertProfile` (navigation)

### New Files
| File | Path | Purpose |
|------|------|---------|
| AlertProfileEntities.cs | `Entities/` | AlertProfile and AlertThreshold entities |
| AlertThresholdDtos.cs | `DTOs/AlertThreshold/` | Request/response DTOs for profiles and thresholds |
| IAlertProfileService.cs | `Services/Interfaces/` | Alert profile service interface |
| AlertProfileService.cs | `Services/` | Alert profile service implementation with validation |
| AlertProfileController.cs | `Controllers/` | REST API endpoints for profile CRUD + assignment |

### API Endpoints
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/v1/alert-profiles` | List profiles (search, pagination, sorted: defaults first then name) |
| GET | `/api/v1/alert-profiles/{id}` | Single profile with thresholds + assignment counts |
| POST | `/api/v1/alert-profiles` | Create profile with optional thresholds |
| PUT | `/api/v1/alert-profiles/{id}` | Update profile name/description/default flag + thresholds |
| DELETE | `/api/v1/alert-profiles/{id}` | Delete profile (fails if assigned to companies or machines) |
| POST | `/api/v1/alert-profiles/{id}/duplicate` | Duplicate profile with thresholds |
| POST | `/api/v1/alert-profiles/{id}/companies` | Assign profile to a company |
| DELETE | `/api/v1/alert-profiles/{id}/companies/{companyId}` | Unassign profile from a company |
| POST | `/api/v1/alert-profiles/{id}/machines` | Assign profile to a machine (override) |
| DELETE | `/api/v1/alert-profiles/{id}/machines/{machineId}` | Remove machine override |

### Validation Rules
- Name is required, max 255 characters
- CPU critical > CPU warning
- RAM critical > RAM warning
- Offline critical > Offline warning
- Failed login critical > failed login warning
- Cannot delete a profile that has assignments
- Cannot unset the only default profile

### Modified Services
- **AlertService.cs**: `EvaluateMachineAlertsAsync` rewritten to resolve the effective profile for a machine (machine override → company profile → default profile → hardcoded fallback) and evaluate thresholds from the resolved profile. Includes deduplication against existing open/acknowledged alerts.
- **AlertProcessor.cs**: Refactored to use `IAlertProfileService.ResolveThresholdsForMachineAsync()` for profile-based thresholds. Retains hardcoded fallback values (90/90/95) when no profile is configured. Fixed rules (Antivirus/Firewall) always fire regardless of profile.

### Exception
- `AlertProfileException` — thrown for validation failures (422), not-found (404), and assignment conflicts

### DI Registration
- `builder.Services.AddScoped<IAlertProfileService, AlertProfileService>()` in Program.cs
