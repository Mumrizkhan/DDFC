# DDFC Identity Seeding Documentation

## Overview
The DDFC Data Seeder has been completely rewritten to properly implement ASP.NET Core Identity with **Role Claims** and **User Claims** for a comprehensive authorization system.

## Changes Made

### 1. **Removed Old Seeding Approach**
- ? Old: Roles had a `Permissions` JSON string property
- ? New: Roles use Identity **Role Claims** for permissions
- ? New: Users have **User Claims** for additional metadata

### 2. **Role Claims Implementation**
All roles now have proper Identity role claims using the `permission` claim type:

| Role | Permissions (Role Claims) |
|------|---------------------------|
| **Admin** | CanManageUsers, CanConfigurePackages, CanViewAllRequests, CanViewReports, CanManageTemplates, CanManageDepartments, CanManageRoles, CanManageSettings, CanResetRoundRobin |
| **Reception Officer** | CanCreateRequest, CanSelectPackage, CanDeliverDocuments |
| **Transfer Officer** | CanApproveTransfer, CanViewAllRequests |
| **Finance Officer** | CanApproveFinance, CanConfirmPayment, CanViewAllRequests |
| **Town Planner** | CanSubmitTownPlanning, CanUploadSoilTest, CanIssuePossessionCert, CanViewAllRequests |
| **Building Control Officer** | CanSubmitBuildingControl, CanViewAllRequests |
| **Architect** | CanUploadPlan, CanViewAllRequests |
| **Structure Engineer** | CanCompleteStructure, CanViewAllRequests |
| **MEP Engineer** | CanCompleteMEP, CanViewAllRequests |
| **Principal Architect** | CanPrincipalApprove, CanViewAllRequests |
| **DHA Design Head** | CanFinalApprove, CanViewAllRequests |
| **Technical Support** | CanManageTickets, CanReplyTickets, CanResolveTickets, CanReopenTickets |

### 3. **User Claims Implementation**
All users have custom claims for enhanced authorization and metadata:

#### Common User Claims (All Staff)
- `userType`: "staff" (distinguishes from customers)
- `departmentCode`: Department code (e.g., "AD", "FD", "TB")

#### Role-Specific User Claims

**Admin:**
- `accessLevel`: "SuperAdmin"

**Reception Officers:**
- `canInitiateRequests`: "true"

**Transfer Officers:**
- `approvalAuthority`: "Transfer"

**Finance Officers:**
- `approvalAuthority`: "Finance"
- `canVerifyPayments`: "true"

**Town Planners:**
- `specialization`: "UrbanPlanning" or "LandUse"

**Building Control Officers:**
- `inspectionAuthority`: "true"

**Architects:**
- `designSpecialty`: "Residential", "Commercial", or "Mixed"
- `yearsOfExperience`: e.g., "5", "7", "8"

**Structure Engineers:**
- `engineeringLicense`: PEC license number (e.g., "PEC-12345")
- `specialization`: "SeismicDesign" or "Foundation"

**MEP Engineers:**
- `engineeringLicense`: PEC license number
- `mepSpecialty`: "HVAC", "Electrical", or "Plumbing"

**Principal Architect:**
- `seniorApprovalAuthority`: "true"
- `yearsOfExperience`: "15"

**DHA Design Head:**
- `finalApprovalAuthority`: "true"
- `rank`: "Brigadier"

**Technical Support:**
- `canManageTickets`: "true"

### 4. **Seeded Users**

#### Administration Department
- **System Administrator** (admin@ddfc.com.pk) - Admin role

#### Front Desk Department
- **Ali Ahmed** (ali.ahmed@ddfc.com.pk) - Reception Officer
- **Fatima Khan** (fatima.khan@ddfc.com.pk) - Reception Officer

#### Transfer Branch
- **Hassan Raza** (hassan.raza@ddfc.com.pk) - Transfer Officer
- **Ayesha Malik** (ayesha.malik@ddfc.com.pk) - Transfer Officer

#### Finance Branch
- **Imran Siddiqui** (imran.siddiqui@ddfc.com.pk) - Finance Officer
- **Sara Baig** (sara.baig@ddfc.com.pk) - Finance Officer

#### Town Planning
- **Kamran Ali** (kamran.ali@ddfc.com.pk) - Town Planner
- **Zainab Hussain** (zainab.hussain@ddfc.com.pk) - Town Planner

#### Building Control
- **Bilal Ahmed** (bilal.ahmed@ddfc.com.pk) - Building Control Officer
- **Maryam Iqbal** (maryam.iqbal@ddfc.com.pk) - Building Control Officer

#### Architecture Department
- **Usman Tariq** (usman.tariq@ddfc.com.pk) - Architect (Residential)
- **Sadia Farooq** (sadia.farooq@ddfc.com.pk) - Architect (Commercial)
- **Adnan Sheikh** (adnan.sheikh@ddfc.com.pk) - Architect (Mixed)

#### Structure Department
- **Fahad Mirza** (fahad.mirza@ddfc.com.pk) - Structure Engineer (Seismic)
- **Hina Javed** (hina.javed@ddfc.com.pk) - Structure Engineer (Foundation)

#### MEP Department
- **Tariq Mahmood** (tariq.mahmood@ddfc.com.pk) - MEP Engineer (HVAC)
- **Nadia Akram** (nadia.akram@ddfc.com.pk) - MEP Engineer (Electrical)
- **Asif Raza** (asif.raza@ddfc.com.pk) - MEP Engineer (Plumbing)

#### Principal Office
- **Jawad Abbas** (jawad.abbas@ddfc.com.pk) - Principal Architect

#### DHA Design Department
- **Brigadier (R) Zulfiqar Ali** (zulfiqar.ali@ddfc.com.pk) - DHA Design Head

#### Technical Support
- **Tech Support Agent** (support@ddfc.com.pk) - Technical Support
- **Omer Siddique** (omer.siddique@ddfc.com.pk) - Technical Support

## Default Passwords

All users have role-specific default passwords:
- **Admin**: `Admin@2026!`
- **Reception Officers**: `Reception@2026!`
- **Transfer Officers**: `Transfer@2026!`
- **Finance Officers**: `Finance@2026!`
- **Town Planners**: `TownPlan@2026!`
- **Building Control Officers**: `BuildControl@2026!`
- **Architects**: `Architect@2026!`
- **Structure Engineers**: `Structure@2026!`
- **MEP Engineers**: `MEP@2026!`
- **Principal Architect**: `Principal@2026!`
- **DHA Design Head**: `DesignHead@2026!`
- **Technical Support**: `Support@2026!`

## Database Schema

### Identity Schema (`identity`)
All Identity tables are now in the `identity` schema:
- `identity.Users`
- `identity.Roles`
- `identity.UserRoles`
- `identity.UserClaims` ? **User-specific claims stored here**
- `identity.RoleClaims` ? **Role permissions stored here**
- `identity.UserLogins`
- `identity.UserTokens`

### DDFC Schema (`ddfc`)
All DDFC business entities remain in the `ddfc` schema:
- `ddfc.Departments`
- `ddfc.Customers`
- `ddfc.Plots`
- `ddfc.PossessionRequests`
- etc.

## Usage in Authorization

### Checking Role Claims (Permissions)
```csharp
// In controller
[Authorize(Policy = "CanApproveTransfer")]

// In authorization handler
var hasPermission = user.HasClaim("permission", "CanApproveTransfer");
```

### Checking User Claims
```csharp
// Get department code
var deptCode = user.FindFirst("departmentCode")?.Value;

// Check specialization
var specialty = user.FindFirst("designSpecialty")?.Value;

// Check approval authority
var hasAuthority = user.HasClaim("approvalAuthority", "Finance");
```

### In JWT Token
The JWT service already includes:
- `role`: Role name
- `userType`: "staff" or "customer"
- `departmentId`: Department GUID

User claims are automatically included in the JWT when the user logs in.

## Benefits

1. ? **Standards Compliant**: Uses ASP.NET Core Identity claims system properly
2. ? **Flexible Authorization**: Can check both role permissions and user-specific claims
3. ? **Scalable**: Easy to add new roles, permissions, or user claims
4. ? **Auditable**: Claims are stored in Identity tables with proper relationships
5. ? **Type-Safe**: Claims can be validated and queried consistently
6. ? **Multi-Department Support**: Multiple users per department with round-robin assignment
7. ? **Realistic Data**: Seeded users have realistic Pakistani names and credentials

## Migration Notes

To apply these changes:

1. **Drop existing database** (if in development):
   ```bash
   dotnet ef database drop --context DDFCDbContext --project src\DDFC.Infrastructure --startup-project src\DDFC.API
   ```

2. **Apply migrations**:
   ```bash
   dotnet ef database update --context DDFCDbContext --project src\DDFC.Infrastructure --startup-project src\DDFC.API
   ```

3. **Run the application**:
   ```bash
   cd src\DDFC.API
   dotnet run
   ```

4. **Verify seeding**: The seeder runs automatically on startup and creates all roles, users, departments, and packages.

## Future Enhancements

- Add more granular permissions
- Implement claims-based policies in `Program.cs`
- Add UI for managing user claims
- Implement department head user claims
- Add territory/region claims for geographic authorization
