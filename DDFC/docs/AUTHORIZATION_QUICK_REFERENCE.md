# Authorization Setup - Quick Reference

## Current Status ?

- Authorization policies: **70+** defined
- Roles: **12** seeded with proper claims
- Permission-based: **Yes** (recommended approach)
- Debug middleware: **Enabled**

## Admin Dashboard Access

### Problem: 403 Forbidden

**Root Cause**: JWT token doesn't have required claim

### Solution Flow

```
1. User logs in
   ?
2. JWT generated with role + permission claims
   ?
3. Client sends JWT in Authorization header
   ?
4. AuthorizationDebugMiddleware logs claims
   ?
5. ASP.NET Core checks policy requirements
   ?
6. If matching claim exists ? 200 OK
   If not ? 403 Forbidden
```

## Admin Role Permissions (After Update)

```
? CanManageUsers
? CanManageCustomers          [NEW]
? CanConfigurePackages
? CanViewAllRequests
? CanViewReports
? CanManageTemplates
? CanManageDepartments
? CanManageRoles
? CanManageSettings
? CanResetRoundRobin
```

## Reception Officer Permissions (After Update)

```
? CanCreateRequest
? CanSelectPackage
? CanDeliverDocuments
? CanViewAllRequests          [NEW]
```

## JWT Token Example

```json
{
  "sub": "user-id",
  "email": "admin@ddfc.com.pk",
  "name": "System Administrator",
  "role": "Admin",
  "permissions": "[\"CanManageUsers\",\"CanManageCustomers\",...]",
  "departmentCode": "AS",
  "userType": "staff",
  "accessLevel": "SuperAdmin",
  "iat": 1234567890,
  "exp": 1234591890
}
```

## Testing Checklist

### 1. Login as Admin
```bash
POST /api/v1/auth/staff/login
Content-Type: application/json

{
  "email": "admin@ddfc.com.pk",
  "password": "Admin@2026!"
}
```

**Expected Response**:
```json
{
  "token": "eyJ0eXAiOiJKV1Q...",
  "user": {
    "userId": "...",
    "fullName": "System Administrator",
    "email": "admin@ddfc.com.pk",
    "roleName": "Admin",
    "permissions": "[\"CanManageUsers\",...]"
  }
}
```

### 2. Check Authorization Logs
Look for:
```
[AuthDebug] Request GET /api/v1/admin/dashboard
[AuthDebug] IsAuthenticated=True
[AuthDebug] Claim: role = Admin
[AuthDebug] Claim: permission = CanManageUsers
[AuthDebug] Claim: permission = CanManageCustomers
```

### 3. Call Admin Dashboard
```bash
GET /api/v1/admin/dashboard
Authorization: Bearer <token>
```

**Expected**: 200 OK with dashboard data
**If 403**: Check logs for missing claims

## Policy Check Flow in Program.cs

```csharp
// Admin role must have this claim:
o.AddPolicy("AdminOnly", p => 
    p.RequireClaim("role", "Admin"));

// Each admin endpoint is protected by:
[Authorize(Policy = "AdminOnly")]
public class AdminController { ... }
```

## Debug Output Meanings

| Log | Meaning |
|-----|---------|
| `IsAuthenticated=True` | JWT validated ? |
| `IsAuthenticated=False` | JWT invalid or missing ? |
| `Claim: role = Admin` | Has admin role ? |
| `Claim: permission = CanManageUsers` | Has permission ? |
| No role claim | Policy check fails ? |

## Common Issues & Fixes

### Issue: 403 Forbidden (role = null)
**Cause**: User not assigned to Admin role
**Fix**: 
```sql
-- Verify in database
SELECT * FROM AspNetUserRoles 
WHERE UserId = '<admin-user-id>'
```

### Issue: 403 Forbidden (role = SomeOtherRole)
**Cause**: User assigned to wrong role
**Fix**: Update user's role assignment

### Issue: 403 Forbidden (no permission claims)
**Cause**: Role has no permission claims in RoleClaims table
**Fix**: Reseed database or manually add role claims

### Issue: 401 Unauthorized
**Cause**: JWT validation failed
**Fix**: 
- Verify JWT signature key matches
- Check token expiration (8 hours)
- Get new token by logging in again

## Files Structure

```
Authorization Setup
??? Program.cs
?   ??? JWT Configuration
?   ??? 70+ Authorization Policies
?   ??? Role & Permission Claims
?
??? JwtService.cs
?   ??? GenerateStaffTokenAsync()
?   ?   ??? Gets user roles
?   ?   ??? Adds role claim
?   ?   ??? Adds permission claims
?   ?   ??? Creates JWT
?   ??? BuildToken()
?       ??? Signs with secret key
?
??? DDFCDataSeeder.cs
?   ??? Seeds 12 roles
?   ??? Adds role claims
?   ??? Creates users
?   ??? Assigns roles to users
?
??? AdminController.cs
?   ??? [Authorize(Policy = "AdminOnly")]
?   ??? Requires: role=Admin
?
??? AuthorizationDebugMiddleware.cs
    ??? Logs all claims for debugging
```

## Role & Permission Matrix

| # | Role | Permissions | Key Endpoints |
|---|------|------------|---------------|
| 1 | Admin | 10 perms (all admin) | `/api/v1/admin/*` |
| 2 | Reception Officer | 4 perms | `/api/v1/requests` (POST) |
| 3 | Transfer Officer | 2 perms | `/api/v1/requests/{id}/transfer/approve` |
| 4 | Finance Officer | 3 perms | `/api/v1/requests/{id}/finance/approve` |
| 5 | Town Planner | 4 perms | `/api/v1/requests/{id}/town-planning` |
| 6 | Building Control Officer | 2 perms | `/api/v1/requests/{id}/building-control` |
| 7 | Architect | 2 perms | `/api/v1/requests/{id}/plan` |
| 8 | Structure Engineer | 2 perms | `/api/v1/requests/{id}/structure/complete` |
| 9 | MEP Engineer | 2 perms | `/api/v1/requests/{id}/mep/complete` |
| 10 | Principal Architect | 2 perms | `/api/v1/requests/{id}/principal-review/approve` |
| 11 | DHA Design Head | 2 perms | `/api/v1/requests/{id}/final-approval/approve` |
| 12 | Technical Support | 4 perms | `/api/v1/tickets/*` |

## Database Tables Involved

```
aspnetusers                    (User profiles)
   ?? userid, email, fullname, etc.
        ?
aspnetuserroles                (User?Role assignment)
   ?? userid, roleid
        ?
aspnetroles                    (Role definitions)
   ?? roleid, name, permissions (JSON)
        ?
aspnetroleclaims               (Role?Permission claims)
   ?? roleid, claimtype="permission", claimvalue="CanManageUsers"
        ?
aspnetuserclaims               (User?Custom claims)
   ?? userid, claimtype, claimvalue
```

## Quick Verification Query

```sql
-- Check Admin user's complete setup
SELECT 
    u.Id, u.Email, u.FullName,
    r.Name as RoleName,
    rc.ClaimValue as Permission
FROM AspNetUsers u
LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id
LEFT JOIN AspNetRoleClaims rc ON r.Id = rc.RoleId
WHERE u.Email = 'admin@ddfc.com.pk'
ORDER BY rc.ClaimValue;
```

Expected output:
```
Id | Email | FullName | RoleName | Permission
...|...|System Administrator|Admin|CanManageUsers
...|...|System Administrator|Admin|CanManageCustomers
...|...|System Administrator|Admin|CanConfigurePackages
... (10 rows total)
```

## Summary

? Authorization implemented with 70+ policies
? 12 roles seeded with proper permissions
? Permission-based approach (best practice)
? JWT tokens include role + permission claims
? Debug middleware logs all claims
? Admin has 10 permissions (including CanManageCustomers)
? Reception Officer has 4 permissions (including CanViewAllRequests)

**To test**: Login as admin ? Get JWT ? Call admin endpoints ? Check logs for claims
