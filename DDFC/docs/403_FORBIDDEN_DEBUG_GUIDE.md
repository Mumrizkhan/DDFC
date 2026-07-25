# 403 Forbidden - Diagnosis & Resolution Guide

## The Problem ??

```
Admin User Login: ? SUCCESS
Get JWT Token: ? SUCCESS
Call Admin Endpoint: ? 403 FORBIDDEN
```

## Why Does This Happen?

### The Authorization Check

```
???????????????????????????????????
?  [AdminController]              ?
?  [Authorize(Policy ="AdminOnly")]
???????????????????????????????????
         ?
         ?
???????????????????????????????????
?  Program.cs Policy Definition   ?
?  "AdminOnly" =                  ?
?    RequireClaim("role", "Admin")?
???????????????????????????????????
         ?
         ? Check: Does user have this claim?
???????????????????????????????????
?  JWT Token Claims (from header) ?
?                                 ?
?  If contains:                   ?
?    {"role": "Admin"}            ?
?      ? ? ALLOW                  ?
?                                 ?
?  If missing or wrong value:     ?
?    ? ? DENY (403 Forbidden)     ?
???????????????????????????????????
```

## Checklist: Why Role Claim Might Be Missing

### ? Problem 1: User Not Assigned to Admin Role

**Database Query:**
```sql
SELECT * FROM AspNetUserRoles
WHERE UserId = (SELECT Id FROM AspNetUsers WHERE Email = 'admin@ddfc.com.pk')
```

**Expected Result:**
```
UserId: <user-guid>
RoleId: <admin-role-guid>
```

**If Empty**: User is not assigned to Admin role
**Fix**: Reseed database or manually insert role assignment

---

### ? Problem 2: Admin Role Exists, But JwtService Not Including It

**Current JwtService Code (should work):**
```csharp
var roleNames = await _userManager.GetRolesAsync(user);
var roleName = roleNames.FirstOrDefault() ?? string.Empty;

var claims = new List<Claim>
{
    // ...
    new("role", roleName),  // ? Should be "Admin"
    // ...
};
```

**If roleName is empty or null**: The claim value will be empty
**Fix**: Check why `GetRolesAsync()` returns empty list

---

### ? Problem 3: Role Claims Not Seeded

**Database Query:**
```sql
SELECT * FROM AspNetRoleClaims
WHERE RoleId = (SELECT Id FROM AspNetRoles WHERE Name = 'Admin')
AND ClaimType = 'permission'
```

**Expected Result:**
```
RoleId: <admin-role-guid>
ClaimType: permission
ClaimValue: CanManageUsers
(10 rows total)
```

**If Empty**: Role has no permission claims
**Fix**: Run seeder again or manually add claims

---

## Complete Verification Query

Run this to see the entire admin setup:

```sql
-- Complete Admin Authorization Check
SELECT 
    u.Id AS UserId,
    u.Email,
    u.FullName,
    u.IsActive,
    u.IsDeleted,
    r.Id AS RoleId,
    r.Name AS RoleName,
    COUNT(rc.Id) AS PermissionCount,
    STRING_AGG(rc.ClaimValue, ', ') AS Permissions,
    ur.RoleId AS AssignmentCheck
FROM AspNetUsers u
LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id
LEFT JOIN AspNetRoleClaims rc ON r.Id = rc.RoleId AND rc.ClaimType = 'permission'
WHERE u.Email = 'admin@ddfc.com.pk'
GROUP BY u.Id, u.Email, u.FullName, u.IsActive, u.IsDeleted, 
         r.Id, r.Name, ur.RoleId
```

## Step-by-Step Debugging

### Step 1: Check Application Logs

**Start API and login**:
```bash
dotnet run --project src/DDFC.API
```

**Look for**:
```
[AuthDebug] Request GET /api/v1/admin/dashboard - Authorization: Bearer eyJ0eXAi...
[AuthDebug] IsAuthenticated=True, Name=System Administrator
[AuthDebug] Claim: role = ?
```

| Output | Meaning |
|--------|---------|
| `role = Admin` | ? Correct |
| `role = ` (empty) | ? Role not in token |
| No "role" line | ? Role claim missing entirely |

---

### Step 2: Decode JWT Token

**Option A: Use jwt.io**
1. Copy token from login response
2. Paste at https://jwt.io
3. Check "payload" section for "role" field

**Option B: Manually inspect**
1. JWT format: `header.payload.signature`
2. Base64 decode the payload
3. Look for `"role": "Admin"`

**Expected payload:**
```json
{
  "sub": "...",
  "email": "admin@ddfc.com.pk",
  "name": "System Administrator",
  "role": "Admin",           // ? THIS MUST BE HERE
  "permission": "[...]",
  "departmentId": "...",
  "userType": "staff",
  "accessLevel": "SuperAdmin",
  "iat": 1234567890,
  "exp": 1234591890
}
```

---

### Step 3: Check Database

```sql
-- 1. User exists and is active
SELECT * FROM AspNetUsers 
WHERE Email = 'admin@ddfc.com.pk'
-- Expected: IsActive = 1, IsDeleted = 0

-- 2. Admin role exists
SELECT * FROM AspNetRoles 
WHERE Name = 'Admin'
-- Expected: 1 row with IsBuiltIn = 1

-- 3. User assigned to Admin role
SELECT * FROM AspNetUserRoles 
WHERE UserId = '<admin-user-id>' 
AND RoleId = '<admin-role-id>'
-- Expected: 1 row

-- 4. Admin role has permission claims
SELECT * FROM AspNetRoleClaims 
WHERE RoleId = '<admin-role-id>'
-- Expected: 10 rows with ClaimType = 'permission'
```

---

### Step 4: Manual Role Claim Verification

```sql
-- Show all permission claims for Admin role
SELECT 
    r.Name AS RoleName,
    rc.ClaimType,
    rc.ClaimValue
FROM AspNetRoles r
LEFT JOIN AspNetRoleClaims rc ON r.Id = rc.RoleId
WHERE r.Name = 'Admin'
ORDER BY rc.ClaimValue
```

**Expected Output:**
```
RoleName | ClaimType   | ClaimValue
---------|-------------|---------------------------
Admin    | permission  | CanConfigurePackages
Admin    | permission  | CanCreateRequest
Admin    | permission  | CanDeliverDocuments
Admin    | permission  | CanFinalApprove
Admin    | permission  | CanIssuePossessionCert
Admin    | permission  | CanManageCustomers
Admin    | permission  | CanManageDepartments
Admin    | permission  | CanManageRoles
Admin    | permission  | CanManageSettings
Admin    | permission  | CanManageTemplates
Admin    | permission  | CanManageUsers
Admin    | permission  | CanResetRoundRobin
Admin    | permission  | CanSelectPackage
Admin    | permission  | CanViewAllRequests
Admin    | permission  | CanViewReports
(15 rows)
```

---

## Common Issues & Solutions

### ? Issue 1: "role" claim is empty

**Symptom:**
```
[AuthDebug] Claim: role = 
```

**Possible Cause:**
- `UserManager.GetRolesAsync()` returned empty
- User not assigned to any role

**Solution:**
```bash
# Reseed database
dotnet ef database drop -s src/DDFC.API
dotnet ef database update -s src/DDFC.API
dotnet run --project src/DDFC.API
```

---

### ? Issue 2: "role" claim missing entirely

**Symptom:**
```
[AuthDebug] Claim: sub = ...
[AuthDebug] Claim: email = ...
[AuthDebug] Claim: name = ...
(no "role" line)
```

**Possible Cause:**
- JwtService not creating "role" claim
- User object doesn't have role information

**Solution:**
Check `JwtService.GenerateStaffTokenAsync()`:
```csharp
new("role", roleName),  // ? Make sure this line exists
```

---

### ? Issue 3: Database not seeded properly

**Symptom:**
- Query returns 0 rows for admin user
- Query returns 0 permission claims

**Solution:**
```bash
# Option 1: Delete and reseed (Local Dev)
dotnet ef database drop -p src/DDFC.Infrastructure -s src/DDFC.API --force
dotnet ef database update -p src/DDFC.Infrastructure -s src/DDFC.API

# Option 2: Manually insert role assignment
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT 
    (SELECT Id FROM AspNetUsers WHERE Email = 'admin@ddfc.com.pk'),
    (SELECT Id FROM AspNetRoles WHERE Name = 'Admin')
```

---

## Complete Recovery Steps

If still getting 403 after updates:

### 1. Clear Everything
```bash
# Delete database
dotnet ef database drop -p src/DDFC.Infrastructure -s src/DDFC.API --force

# Verify deleted
# (Check SQL Server)
```

### 2. Rebuild Database
```bash
# Create fresh from migrations
dotnet ef database update -p src/DDFC.Infrastructure -s src/DDFC.API
```

### 3. Start API (Triggers Seeder)
```bash
dotnet run --project src/DDFC.API
```

**Watch for**:
```
info: Seeding DDFC database...
info: Admin role created with 10 permissions
info: Admin user created
```

### 4. Test Authorization
```bash
# Terminal 1: Keep API running
dotnet run --project src/DDFC.API

# Terminal 2: Test login
curl -X POST http://localhost:5000/api/v1/auth/staff/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@ddfc.com.pk","password":"Admin@2026!"}'

# Copy token from response
```

### 5. Check Logs
Look for:
```
[AuthDebug] Claim: role = Admin      ?
[AuthDebug] Claim: permission = CanManageUsers
[AuthDebug] Claim: permission = CanManageCustomers
...
```

### 6. Test Dashboard
```bash
curl -H "Authorization: Bearer <token>" \
  http://localhost:5000/api/v1/admin/dashboard
```

**Expected**: 200 OK with dashboard data

---

## Prevention Checklist

- [x] Admin role has "role" claim in JWT
- [x] User assigned to Admin role in database
- [x] Admin role has all permission claims
- [x] JwtService includes role in token
- [x] AuthorizationDebugMiddleware logs claims
- [x] Policy checks for correct claim

---

## Support

If still stuck after all steps:

1. **Share debug logs** - What do `[AuthDebug]` lines show?
2. **Share SQL query results** - Verify database state
3. **Share JWT payload** - What claims are actually in token?
4. **Share policy definition** - Confirm Program.cs setup

With this information, we can pinpoint exact issue.

---

**Remember**: 403 Forbidden = Claims don't match requirements
Always check: Does JWT have the claim? Does policy require it?
