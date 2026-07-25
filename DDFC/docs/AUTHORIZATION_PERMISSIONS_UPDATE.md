# Authorization Update Summary

## Changes Made

### 1. Added `CanManageCustomers` Permission

**File:** `src/DDFC.API/Program.cs`
- Added new permission-based policy: `CanManageCustomers`
- Required claim: `permission: "CanManageCustomers"`

**File:** `src/DDFC.Infrastructure/Data/DDFCDataSeeder.cs`
- Added `CanManageCustomers` to Admin role claims
- Admin role now has 10 permissions (was 9)

### 2. Enhanced Reception Officer Role

**File:** `src/DDFC.Infrastructure/Data/DDFCDataSeeder.cs`
- Added `CanViewAllRequests` permission to Reception Officer role
- Reception Officer now has 4 permissions (was 3):
  - `CanCreateRequest`
  - `CanSelectPackage`
  - `CanDeliverDocuments`
  - `CanViewAllRequests` (NEW)

### 3. Authorization Policies Structure

#### Admin Role Permissions (10 total)
```json
{
  "role": "Admin",
  "permissions": [
    "CanManageUsers",
    "CanManageCustomers",
    "CanConfigurePackages",
    "CanViewAllRequests",
    "CanViewReports",
    "CanManageTemplates",
    "CanManageDepartments",
    "CanManageRoles",
    "CanManageSettings",
    "CanResetRoundRobin"
  ]
}
```

#### Reception Officer Role Permissions (4 total)
```json
{
  "role": "Reception Officer",
  "permissions": [
    "CanCreateRequest",
    "CanSelectPackage",
    "CanDeliverDocuments",
    "CanViewAllRequests"
  ]
}
```

## JWT Token Claims

When an Admin user logs in, the JWT token will include:

```json
{
  "sub": "user-guid",
  "email": "admin@ddfc.com.pk",
  "name": "System Administrator",
  "role": "Admin",
  "permissions": "[\"CanManageUsers\",\"CanManageCustomers\",\"CanConfigurePackages\",...]",
  "departmentCode": "AS",
  "userType": "staff",
  "accessLevel": "SuperAdmin"
}
```

## Authorization Flow

1. **Authentication**: User logs in with email/password
2. **Token Generation**: JWT created with role and permission claims
3. **Request**: Client sends JWT in Authorization header
4. **Middleware**: `AuthorizationDebugMiddleware` logs claims
5. **Policy Check**: ASP.NET Core checks if user has required claim
6. **Access Decision**: Grant access if claim matches, return 403 if not

## Testing the Admin Dashboard

### Prerequisites
- Database seeded with Admin user
- Admin user assigned to "Admin" role

### Steps
1. Login: `POST /api/v1/auth/staff/login`
   ```json
   {
     "email": "admin@ddfc.com.pk",
     "password": "Admin@2026!"
   }
   ```

2. Copy JWT token from response

3. Access dashboard: `GET /api/v1/admin/dashboard`
   - Header: `Authorization: Bearer <token>`
   - Expected: 200 OK

4. Check logs for:
   ```
   [AuthDebug] Claim: role = Admin
   [AuthDebug] Claim: permission = CanManageUsers
   [AuthDebug] Claim: permission = CanManageCustomers
   ...
   ```

## Role Permission Mapping

| Role | Permissions | Controllers |
|------|-------------|-------------|
| Admin | 10 (all admin permissions) | `/api/v1/admin/*` |
| Reception Officer | 4 (create, select, deliver, view) | `/api/v1/requests/` (create) |
| Transfer Officer | 2 (approve transfer, view all) | `/api/v1/requests/{id}/transfer/approve` |
| Finance Officer | 3 (approve finance, confirm payment, view all) | `/api/v1/requests/{id}/finance/approve` |
| Town Planner | 4 (submit planning, soil test, cert, view all) | `/api/v1/requests/{id}/town-planning` |
| Building Control Officer | 2 (submit building control, view all) | `/api/v1/requests/{id}/building-control` |
| Architect | 2 (upload plan, view all) | `/api/v1/requests/{id}/plan` |
| Structure Engineer | 2 (complete structure, view all) | `/api/v1/requests/{id}/structure/complete` |
| MEP Engineer | 2 (complete MEP, view all) | `/api/v1/requests/{id}/mep/complete` |
| Principal Architect | 2 (principal approve, view all) | `/api/v1/requests/{id}/principal-review/approve` |
| DHA Design Head | 2 (final approve, view all) | `/api/v1/requests/{id}/final-approval/approve` |
| Technical Support | 4 (manage, reply, resolve, reopen tickets) | `/api/v1/tickets/` (support endpoints) |

## Database Seeding

To apply these changes:

1. **Delete existing database** (if testing locally):
   ```bash
   # Option A: Drop and recreate
   dotnet ef database drop --project src/DDFC.Infrastructure -s src/DDFC.API
   dotnet ef database update --project src/DDFC.Infrastructure -s src/DDFC.API
   ```

2. **Or run migrations** (in production):
   ```bash
   dotnet ef migrations add AddCanManageCustomersPermission --project src/DDFC.Infrastructure -s src/DDFC.API
   dotnet ef database update --project src/DDFC.Infrastructure -s src/DDFC.API
   ```

3. **Verify seeding**:
   ```bash
   dotnet run --project src/DDFC.API
   ```

## Authorization Debug Logs

The `AuthorizationDebugMiddleware` logs all incoming requests with:
- Authorization header value
- Authentication status
- All user claims

**Example log output**:
```
[AuthDebug] Request GET /api/v1/admin/dashboard - Authorization: Bearer eyJ0eXAiOiJKV1Q...
[AuthDebug] IsAuthenticated=True, Name=System Administrator
[AuthDebug] Claim: sub = <user-guid>
[AuthDebug] Claim: email = admin@ddfc.com.pk
[AuthDebug] Claim: name = System Administrator
[AuthDebug] Claim: role = Admin
[AuthDebug] Claim: permissions = ["CanManageUsers","CanManageCustomers",...]
[AuthDebug] Claim: departmentCode = AS
[AuthDebug] Claim: userType = staff
[AuthDebug] Claim: accessLevel = SuperAdmin
```

## Troubleshooting

### 403 Forbidden on Admin Endpoints

**Possible causes**:
1. JWT token missing `role: "Admin"` claim
   - Check debug logs for claim presence
   - Verify user assigned to Admin role in database

2. User not assigned to Admin role
   - Check `AspNetUserRoles` table
   - Verify role name is exactly "Admin"

3. Token expired or invalid
   - Get a fresh token by logging in again
   - Check token expiration (8 hours by default)

### Solution Steps
1. Check middleware logs: `[AuthDebug] Claim: role = ?`
2. Verify database: `SELECT * FROM AspNetUserRoles WHERE UserId = ?`
3. Regenerate token if needed
4. Clear browser cache/cookies

## Files Modified

1. **src/DDFC.API/Program.cs**
   - Added `CanManageCustomers` policy

2. **src/DDFC.Infrastructure/Data/DDFCDataSeeder.cs**
   - Updated Admin role claims
   - Updated Reception Officer role claims

3. **src/DDFC.API/Debugging/AuthorizationDebugMiddleware.cs** (already created)
   - Logs authorization details for debugging

## Verification Checklist

- [x] `CanManageCustomers` policy defined in Program.cs
- [x] Admin role has `CanManageCustomers` claim
- [x] Reception Officer role has `CanViewAllRequests` claim
- [x] Code compiles successfully
- [x] No runtime errors
- [x] Debug middleware enabled

## Next Steps

1. Test Admin login and dashboard access
2. Verify all role permissions in debug logs
3. Test other roles (Reception, Transfer, etc.)
4. Monitor logs for authorization issues
5. Update API documentation with permission matrix
