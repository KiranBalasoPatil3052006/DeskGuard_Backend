# DeskGuard Backend - Database Context

## Account Management Module

### Table: `users` (extended)

The existing `users` table has been extended with two new columns for account management.

#### New Columns

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `employee_id` | varchar(20) | UNIQUE, nullable | Employee ID in EMP-XXXX format (e.g., EMP-0001) |
| `created_by_user_id` | bigint | FK → `users(id)`, nullable, SET NULL on delete | Who created this admin account |

#### Indexes
- `ix_users_employee_id` — UNIQUE on `employee_id`

#### Relationships
- `users.created_by_user_id` → `users.id` (self-referencing FK, SET NULL on delete)

### Seed Data

#### Roles
| Id | Name | Guard Name |
|----|------|------------|
| 1 | Super Admin | web |
| 2 | Company Admin | web |
| 3 | Support Technician | web |
| **4** | **Admin** | **web** |

#### Default User
| Field | Value |
|-------|-------|
| Email | kiranbalasopatil33@gmail.com |
| Name | Kiran Balaso Patil |
| Phone | 6846810210 |
| Employee ID | EMP-0001 |
| Role | Super Admin |

### Soft Delete
- Accounts are soft-deleted by setting `deleted_at` timestamp
- Global query filter `u.DeletedAt == null` excludes deleted records from all queries
- The `is_deleted` boolean approach was **not used** — the existing `DeletedAt` timestamp pattern is followed

### Migration
- **Migration name**: `20260714160917_AddEmployeeIdToUsers`
- Adds `employee_id` and `created_by_user_id` columns + unique index

---

## Alert Threshold Management Module

### Overview
The Alert Threshold Management module adds profile-based threshold evaluation. Profiles define when alerts should trigger for different types of systems, replacing the previous flat per-company alert rule system.

### New Tables

#### Table: `alert_profiles`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `id` | bigint | PK, auto-increment | Primary key |
| `name` | varchar(255) | NOT NULL | Profile display name |
| `description` | text | nullable | Optional description |
| `is_default` | boolean | default false | Whether this is the global default profile |
| `created_at` | timestamptz | NOT NULL | Record creation timestamp |
| `updated_at` | timestamptz | NOT NULL | Record last update timestamp |

#### Table: `alert_thresholds`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `id` | bigint | PK, auto-increment | Primary key |
| `profile_id` | bigint | FK → `alert_profiles(id)`, CASCADE | Parent profile |
| `cpu_warning_percent` | decimal(5,2) | nullable | CPU warning threshold |
| `cpu_critical_percent` | decimal(5,2) | nullable | CPU critical threshold |
| `cpu_warning_duration_minutes` | int | nullable | Duration before CPU warning triggers |
| `ram_warning_percent` | decimal(5,2) | nullable | RAM warning threshold |
| `ram_critical_percent` | decimal(5,2) | nullable | RAM critical threshold |
| `ram_warning_duration_minutes` | int | nullable | Duration before RAM warning triggers |
| `cpu_temp_warning` | decimal(5,2) | nullable | CPU temperature warning °C |
| `cpu_temp_critical` | decimal(5,2) | nullable | CPU temperature critical °C |
| `disk_warning_percent` | decimal(5,2) | nullable | Disk usage warning threshold |
| `disk_critical_percent` | decimal(5,2) | nullable | Disk usage critical threshold |
| `disk_smart_warning_enabled` | boolean | nullable | Enable SMART warning alerts |
| `disk_smart_critical_enabled` | boolean | nullable | Enable SMART critical alerts |
| `offline_warning_minutes` | int | nullable | Offline duration before warning |
| `offline_critical_minutes` | int | nullable | Offline duration before critical |
| `failed_login_warning_count` | int | nullable | Failed login count warning |
| `failed_login_critical_count` | int | nullable | Failed login count critical |
| `network_disconnect_warning_count` | int | nullable | Network disconnect warning |
| `created_at` | timestamptz | NOT NULL | Record creation timestamp |
| `updated_at` | timestamptz | NOT NULL | Record last update timestamp |

### Modified Tables

#### Table: `companies`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `alert_profile_id` | bigint | FK → `alert_profiles(id)`, SET NULL | Profile assigned to this company |

#### Table: `machines`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `custom_alert_profile_id` | bigint | FK → `alert_profiles(id)`, SET NULL | Profile override for this specific machine |

### Profile Resolution Order
1. Machine-level override (`machines.custom_alert_profile_id`)
2. Company-level assignment (`companies.alert_profile_id`)
3. Global default profile (`alert_profiles.is_default = true`)
4. Fallback hardcoded values (90% CPU, 90% RAM, 95% Disk)

### Seed Data
| Profile ID | Name | Description |
|------------|------|-------------|
| 1 | Default Profile | Standard monitoring thresholds for general office workstations (is_default = true) |
| 2 | Office Workstations | Optimized for typical office productivity workloads |
| 3 | Development Machines | Relaxed thresholds for developer machines with high resource usage |

### Fixed Rules (Always Enabled)
- RAM Changed, SSD Changed, HDD Changed, CPU Changed, Motherboard Changed, BIOS Changed
- Antivirus Removed, Firewall Disabled
- These are mandatory for security and AMC auditing. They are not driven by profiles.

### Migration
- **Migration name**: `20260715081217_AddAlertProfiles`
- Creates both `alert_profiles` and `alert_thresholds` tables
- Adds FK columns to `companies` and `machines`
- Seeds 3 default profiles
