# DDFC Authorization Policies Reference

## Overview
The DDFC API now implements a comprehensive **claims-based authorization** system with 70+ policies covering permissions, user attributes, departments, and complex scenarios.

## Policy Categories

### 1. Basic User Type Policies

| Policy Name | Description | Required Claim |
|-------------|-------------|----------------|
| `AdminOnly` | Admin users only | `role: "Admin"` |
| `StaffOnly` | Any staff member | `userType: "staff"` |
| `StaffOrAdmin` | Staff or Admin | `userType: "staff"` OR `role: "Admin"` |
| `CustomerOnly` | Customers only | `userType: "customer"` |

**Usage:**
```csharp
[Authorize(Policy = "StaffOnly")]
public async Task<IActionResult> GetStaffDashboard()
```

---

### 2. Role-Based Policies (Backward Compatibility)

| Policy Name | Role Required |
|-------------|---------------|
| `ReceptionOfficer` | Reception Officer |
| `TransferOfficer` | Transfer Officer |
| `FinanceOfficer` | Finance Officer |
| `TownPlanner` | Town Planner |
| `BuildingControlOfficer` | Building Control Officer |
| `Architect` | Architect |
| `StructureEngineer` | Structure Engineer |
| `MEPEngineer` | MEP Engineer |
| `PrincipalArchitect` | Principal Architect |
| `DesignHead` | DHA Design Head |
| `TechnicalSupport` | Technical Support |

**Usage:**
```csharp
[Authorize(Policy = "Architect")]
public async Task<IActionResult> UploadArchitecturalPlan()
```

?? **Note:** These are provided for backward compatibility. **Permission-based policies** (below) are recommended.

---

### 3. Permission-Based Policies ? RECOMMENDED

#### Admin Permissions

| Policy Name | Description | Seeded Roles |
|-------------|-------------|--------------|
| `CanManageUsers` | Create, update, delete users | Admin |
| `CanConfigurePackages` | Manage service packages | Admin |
| `CanViewAllRequests` | View all possession requests | Admin, All Officers |
| `CanViewReports` | Access reports and analytics | Admin |
| `CanManageTemplates` | Manage document templates | Admin |
| `CanManageDepartments` | Manage departments | Admin |
| `CanManageRoles` | Manage roles and permissions | Admin |
| `CanManageSettings` | System configuration | Admin |
| `CanResetRoundRobin` | Reset round-robin assignment | Admin |

**Usage:**
```csharp
[Authorize(Policy = "CanManageUsers")]
[HttpPost("users")]
public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
```

#### Reception Permissions

| Policy Name | Description | Seeded Roles |
|-------------|-------------|--------------|
| `CanCreateRequest` | Initiate possession requests | Reception Officer |
| `CanSelectPackage` | Select service packages | Reception Officer |
| `CanDeliverDocuments` | Handle document delivery | Reception Officer |

#### Transfer & Finance Permissions

| Policy Name | Description | Seeded Roles |
|-------------|-------------|--------------|
| `CanApproveTransfer` | Approve transfer documents | Transfer Officer |
| `CanApproveFinance` | Financial approval | Finance Officer |
| `CanConfirmPayment` | Confirm payments | Finance Officer |

#### Planning & Control Permissions

| Policy Name | Description | Seeded Roles |
|-------------|-------------|--------------|
| `CanSubmitTownPlanning` | Submit town planning documents | Town Planner |
| `CanSubmitBuildingControl` | Submit building control reports | Building Control Officer |
| `CanUploadSoilTest` | Upload soil test reports | Town Planner |
| `CanIssuePossessionCert` | Issue possession certificates | Town Planner |

#### Design & Engineering Permissions

| Policy Name | Description | Seeded Roles |
|-------------|-------------|--------------|
| `CanUploadPlan` | Upload architectural plans | Architect |
| `CanCompleteStructure` | Complete structural reports | Structure Engineer |
| `CanCompleteMEP` | Complete MEP reports | MEP Engineer |
| `CanPrincipalApprove` | Principal architect approval | Principal Architect |
| `CanFinalApprove` | Final design approval | DHA Design Head |

#### Support Permissions

| Policy Name | Description | Seeded Roles |
|-------------|-------------|--------------|
| `CanManageTickets` | Create and manage tickets | Technical Support |
| `CanReplyTickets` | Reply to support tickets | Technical Support |
| `CanResolveTickets` | Resolve tickets | Technical Support |
| `CanReopenTickets` | Reopen closed tickets | Technical Support |

**Usage:**
```csharp
[Authorize(Policy = "CanUploadPlan")]
[HttpPost("architectural-plans")]
public async Task<IActionResult> UploadPlan([FromBody] PlanDto dto)
```

---

### 4. User Claim-Based Policies (User-Specific Attributes)

#### Approval Authorities

| Policy Name | Description | User Claim Required |
|-------------|-------------|---------------------|
| `HasApprovalAuthority` | Any approval authority | `approvalAuthority` exists |
| `TransferApprovalAuthority` | Transfer approval | `approvalAuthority: "Transfer"` |
| `FinanceApprovalAuthority` | Finance approval | `approvalAuthority: "Finance"` |

**Seeded Users:**
- Hassan Raza, Ayesha Malik ? Transfer
- Imran Siddiqui, Sara Baig ? Finance

**Usage:**
```csharp
[Authorize(Policy = "TransferApprovalAuthority")]
public async Task<IActionResult> ApproveTransferDocument()
```

#### Engineering & Professional

| Policy Name | Description | User Claim Required |
|-------------|-------------|---------------------|
| `HasEngineeringLicense` | Has PEC license | `engineeringLicense` exists |
| `InspectionAuthority` | Can perform inspections | `inspectionAuthority: "true"` |

**Seeded Users:**
- Fahad Mirza, Hina Javed (Structure) ? PEC licenses
- Tariq Mahmood, Nadia Akram, Asif Raza (MEP) ? PEC licenses
- Bilal Ahmed, Maryam Iqbal ? Inspection authority

#### Senior Authorities

| Policy Name | Description | User Claim Required |
|-------------|-------------|---------------------|
| `SeniorApprovalAuthority` | Senior approval | `seniorApprovalAuthority: "true"` |
| `FinalApprovalAuthority` | Final approval | `finalApprovalAuthority: "true"` |
| `SuperAdmin` | Super admin access | `accessLevel: "SuperAdmin"` |

**Seeded Users:**
- Jawad Abbas ? Senior approval
- Brigadier Zulfiqar Ali ? Final approval
- System Administrator ? Super admin

---

### 5. Department-Based Policies

| Policy Name | Department | Code |
|-------------|------------|------|
| `FrontDeskDepartment` | Front Desk | FD |
| `TransferDepartment` | Transfer Branch | TB |
| `FinanceDepartment` | Finance Branch | FB |
| `TownPlanningDepartment` | Town Planning | TP |
| `BuildingControlDepartment` | Building Control | BC |
| `ArchitectureDepartment` | Architecture Dept | AD |
| `StructureDepartment` | Structure Dept | SD |
| `MEPDepartment` | MEP Dept | MD |
| `PrincipalOfficeDepartment` | Principal Office | PO |
| `DHADesignDepartment` | DHA Design Dept | DD |
| `AdministrationDepartment` | Administration | AS |
| `TechnicalSupportDepartment` | Technical Support | TS |

**Usage:**
```csharp
[Authorize(Policy = "ArchitectureDepartment")]
public async Task<IActionResult> GetDepartmentTasks()
```

---

### 6. Combined/Complex Policies

#### Senior Architects
**Policy:** `SeniorArchitect`  
**Description:** Principal Architect OR DHA Design Head  
**Logic:** Has `CanPrincipalApprove` OR `CanFinalApprove` permission

```csharp
[Authorize(Policy = "SeniorArchitect")]
public async Task<IActionResult> ReviewMajorProject()
```

#### Licensed Engineers
**Policy:** `LicensedEngineer`  
**Description:** Structure or MEP Engineer with PEC license  
**Logic:** Has structure/MEP permission AND `engineeringLicense` claim

```csharp
[Authorize(Policy = "LicensedEngineer")]
public async Task<IActionResult> SignEngineeeringReport()
```

#### Design Specialists
**Policy:** `DesignSpecialist`  
**Description:** Any design team member (Architect or Engineer)  
**Logic:** Has `CanUploadPlan`, `CanCompleteStructure`, OR `CanCompleteMEP`

```csharp
[Authorize(Policy = "DesignSpecialist")]
public async Task<IActionResult> AccessDesignTools()
```

#### Approval Chain
**Policy:** `CanApprove`  
**Description:** Anyone with any approval permission  
**Logic:** Has any approval permission (Transfer, Finance, Principal, Final)

```csharp
[Authorize(Policy = "CanApprove")]
public async Task<IActionResult> GetPendingApprovals()
```

#### Payment Handlers
**Policy:** `CanHandlePayments`  
**Description:** Can confirm or verify payments  
**Logic:** Has `CanConfirmPayment` permission OR `canVerifyPayments` claim

```csharp
[Authorize(Policy = "CanHandlePayments")]
public async Task<IActionResult> ProcessPayment()
```

#### Request Initiators
**Policy:** `CanInitiateRequests`  
**Description:** Can create new possession requests  
**Logic:** Has `CanCreateRequest` permission OR `canInitiateRequests` claim

```csharp
[Authorize(Policy = "CanInitiateRequests")]
public async Task<IActionResult> CreatePossessionRequest()
```

#### Technical Team
**Policy:** `TechnicalTeam`  
**Description:** Any technical department member  
**Departments:** AD, SD, MD, PO, DD

```csharp
[Authorize(Policy = "TechnicalTeam")]
public async Task<IActionResult> AccessTechnicalResources()
```

#### Operational Team
**Policy:** `OperationalTeam`  
**Description:** Front desk, Transfer, Finance  
**Departments:** FD, TB, FB

```csharp
[Authorize(Policy = "OperationalTeam")]
public async Task<IActionResult> GetOperationalDashboard()
```

#### Regulatory Team
**Policy:** `RegulatoryTeam`  
**Description:** Town Planning, Building Control  
**Departments:** TP, BC

```csharp
[Authorize(Policy = "RegulatoryTeam")]
public async Task<IActionResult> GetRegulatoryCompliance()
```

#### Experienced Staff
**Policy:** `ExperiencedStaff`  
**Description:** Staff with 5+ years experience  
**Logic:** `yearsOfExperience` claim value >= 5

```csharp
[Authorize(Policy = "ExperiencedStaff")]
public async Task<IActionResult> MentorJuniorStaff()
```

**Seeded Users (5+ years):**
- Sadia Farooq (7 years) - Architect
- Adnan Sheikh (8 years) - Architect
- Jawad Abbas (15 years) - Principal Architect

---

## Usage Examples

### Single Policy
```csharp
[Authorize(Policy = "CanApproveTransfer")]
[HttpPost("approve-transfer/{id}")]
public async Task<IActionResult> ApproveTransfer(Guid id)
{
    // Only Transfer Officers can access
}
```

### Multiple Policies (AND)
```csharp
[Authorize(Policy = "CanCompleteStructure")]
[Authorize(Policy = "HasEngineeringLicense")]
[HttpPost("sign-structural-report")]
public async Task<IActionResult> SignReport()
{
    // Only licensed structure engineers can access
}
```

### Conditional Logic in Code
```csharp
[Authorize(Policy = "StaffOnly")]
public async Task<IActionResult> GetRequest(Guid id)
{
    var request = await _service.GetRequestAsync(id);
    
    // Check if user can view all requests OR owns this request
    var canViewAll = User.HasClaim("permission", "CanViewAllRequests");
    var isOwner = User.FindFirst(ClaimTypes.NameIdentifier)?.Value == request.CreatedBy;
    
    if (!canViewAll && !isOwner)
        return Forbid();
    
    return Ok(request);
}
```

### Accessing Claims in Services
```csharp
public class MyService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public MyService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public async Task<string> GetUserDepartment()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user?.FindFirst("departmentCode")?.Value ?? "Unknown";
    }
    
    public async Task<bool> HasSpecialization(string specialty)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var userSpecialty = user?.FindFirst("designSpecialty")?.Value;
        return userSpecialty == specialty;
    }
}
```

---

## Policy Decision Matrix

### Who Can Do What?

| Action | Policies | Seeded Users |
|--------|----------|--------------|
| **Create User** | `CanManageUsers` | Admin |
| **Configure Packages** | `CanConfigurePackages` | Admin |
| **Initiate Request** | `CanCreateRequest`, `CanInitiateRequests` | Reception Officers |
| **Approve Transfer** | `CanApproveTransfer`, `TransferApprovalAuthority` | Transfer Officers |
| **Confirm Payment** | `CanConfirmPayment`, `CanHandlePayments` | Finance Officers |
| **Upload Architecture Plan** | `CanUploadPlan` | Architects |
| **Sign Structure Report** | `LicensedEngineer` | Fahad Mirza, Hina Javed |
| **Sign MEP Report** | `LicensedEngineer` | Tariq Mahmood, Nadia Akram, Asif Raza |
| **Principal Approval** | `CanPrincipalApprove`, `SeniorArchitect` | Jawad Abbas |
| **Final Approval** | `CanFinalApprove`, `FinalApprovalAuthority` | Brigadier Zulfiqar Ali |
| **Manage Tickets** | `CanManageTickets` | Tech Support |
| **View All Requests** | `CanViewAllRequests` | Admin, All Officers |

---

## Migration Guide

### Old Approach (Deprecated)
```csharp
// ? Old: Role-based only
[Authorize(Roles = "Architect")]
public async Task<IActionResult> UploadPlan()
```

### New Approach (Recommended)
```csharp
// ? New: Permission-based
[Authorize(Policy = "CanUploadPlan")]
public async Task<IActionResult> UploadPlan()
```

**Benefits:**
- ? More granular control
- ? Multiple roles can have same permission
- ? Permissions can be added/removed without role changes
- ? User-specific attributes (claims) for fine-grained authorization

---

## Testing Authorization

### Test with Seeded Users

```bash
# Login as Admin
POST /api/v1/auth/staff/login
{
  "email": "admin@ddfc.com.pk",
  "password": "Admin@2026!"
}

# Login as Architect
POST /api/v1/auth/staff/login
{
  "email": "usman.tariq@ddfc.com.pk",
  "password": "Architect@2026!"
}

# Login as Transfer Officer
POST /api/v1/auth/staff/login
{
  "email": "hassan.raza@ddfc.com.pk",
  "password": "Transfer@2026!"
}
```

### Expected Results

| User | Policy | Result |
|------|--------|--------|
| admin@ddfc.com.pk | `CanManageUsers` | ? Authorized |
| usman.tariq@ddfc.com.pk | `CanUploadPlan` | ? Authorized |
| hassan.raza@ddfc.com.pk | `CanApproveTransfer` | ? Authorized |
| usman.tariq@ddfc.com.pk | `CanApproveTransfer` | ? 403 Forbidden |
| support@ddfc.com.pk | `CanUploadPlan` | ? 403 Forbidden |

---

## Best Practices

### 1. Use Permission-Based Policies
```csharp
// ? Good
[Authorize(Policy = "CanUploadPlan")]

// ? Avoid
[Authorize(Roles = "Architect")]
```

### 2. Combine Policies for Complex Scenarios
```csharp
[Authorize(Policy = "CanCompleteStructure")]
[Authorize(Policy = "HasEngineeringLicense")]
public async Task<IActionResult> SignReport()
```

### 3. Use Claims for Business Logic
```csharp
var specialty = User.FindFirst("designSpecialty")?.Value;
if (specialty == "Residential")
{
    return await ProcessResidentialDesign();
}
```

### 4. Check Claims Programmatically When Needed
```csharp
if (!User.HasClaim("permission", "CanViewAllRequests"))
{
    // Filter to user's own requests only
    query = query.Where(r => r.CreatedBy == userId);
}
```

---

## Summary

- **70+ Policies** covering all authorization scenarios
- **Permission-based** policies for flexible access control
- **User claim-based** policies for user-specific attributes
- **Department-based** policies for organizational structure
- **Complex policies** for multi-criteria authorization
- **25 seeded users** with proper claims across 12 departments
- **Role claims** stored in `identity.RoleClaims`
- **User claims** stored in `identity.UserClaims`

**All policies are production-ready and fully documented!** ??
