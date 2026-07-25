# ?? Complete JWT & Authorization Flow - Technical Guide

## System Architecture Overview

```
???????????????????????????????????????????????????????????????????
?                    AUTHORIZATION SYSTEM                         ?
???????????????????????????????????????????????????????????????????
?                                                                 ?
?  Role Claims (Database)                JWT Token               ?
?  ?? Admin                              ?? role: "Admin"       ?
?  ?  ?? permission: CanManageUsers      ?? permission: Can...  ?
?  ?  ?? permission: CanManageCustomers  ?? permission: Can...  ?
?  ?  ?? ... (10 total)                  ?? ... (all 10)        ?
?  ?                                                             ?
?  ?? Other Roles...                   Policy Check:            ?
?     ?? Reception Officer              RequireClaim(           ?
?     ?? Transfer Officer               "permission",           ?
?     ?? Finance Officer                "CanManageUsers")       ?
?     ?? ...                                                     ?
?                                        Endpoint Access:       ?
???????????????????????????????????????????????????????????????????
```

---

## 1. Database Layer - Role Claims Storage

### AspNetRoles Table
```
Id: {GUID}
Name: "Admin"
IsBuiltIn: 1
```

### AspNetRoleClaims Table
```
RoleId: {Admin Role ID}
ClaimType: "permission"
ClaimValue: "CanManageUsers"

RoleId: {Admin Role ID}
ClaimType: "permission"
ClaimValue: "CanManageCustomers"

... (10 rows for Admin role)
```

### AspNetUserRoles Table
```
UserId: {Admin User ID}
RoleId: {Admin Role ID}
```

---

## 2. JWT Generation Process

### Step-by-Step Flow

```
1. User Login
   ?? Email: admin@ddfc.com.pk
   ?? Password: Admin@2026!
         ?
         ?
2. Validate Credentials
   ?? Find user by email
   ?? Check password hash
   ?? Check IsActive, !IsDeleted
         ?
         ?
3. Get User Roles
   ?? Query: SELECT RoleId FROM AspNetUserRoles 
   ?         WHERE UserId = {user.Id}
   ?? Result: [Admin Role ID]
         ?
         ?
4. For Each Role, Get Permission Claims
   ?? Query: SELECT ClaimValue FROM AspNetRoleClaims
   ?         WHERE RoleId = {Admin Role ID}
   ?         AND ClaimType = 'permission'
   ?? Result: [CanManageUsers, CanManageCustomers, ...]
         ?
         ?
5. Build Claims List for JWT
   ?? sub: {user.Id}
   ?? email: admin@ddfc.com.pk
   ?? name: System Administrator
   ?? role: Admin
   ?? departmentId: {dept.Id}
   ?? userType: staff
   ?? permission: CanManageUsers
   ?? permission: CanManageCustomers
   ?? permission: CanConfigurePackages
   ?? ... (all permissions)
   ?? iat, exp (issued at, expiration)
         ?
         ?
6. Sign Token with Secret Key
   ?? Header: {typ: JWT, alg: HS256}
   ?? Payload: {all claims above}
   ?? Signature: HMACSHA256(header.payload, secret)
   ?? Result: header.payload.signature
         ?
         ?
7. Return to Client
   ?? token: eyJ0eXAiOiJKV1Q...
   ?? user: {userId, fullName, email, roleName, permissions}
```

---

## 3. Authorization Process

### JWT Received in Request

```
Request: GET /api/v1/admin/dashboard
Header: Authorization: Bearer eyJ0eXAiOiJKV1Q...
```

### Authorization Middleware Flow

```
1. Extract Token from Header
   ?? "Authorization: Bearer <token>"
         ?
         ?
2. Validate JWT Signature
   ?? Extract header, payload, signature
   ?? Recalculate: HMACSHA256(header.payload, secret)
   ?? Compare with provided signature
   ?? If ? valid, continue; If ? invalid, return 401
         ?
         ?
3. Parse Claims from Payload
   ?? Extract all claims from JWT payload
   ?? Create ClaimsPrincipal object
   ?? Attach to HttpContext.User
   ?? Result: User has claims:
      ?? sub: {user-id}
      ?? email: admin@ddfc.com.pk
      ?? name: System Administrator
      ?? role: Admin
      ?? permission: CanManageUsers
      ?? permission: CanManageCustomers
      ?? ... (all permissions)
      ?? (ready for policy checks)
         ?
         ?
4. Debug Middleware (Optional)
   ?? Log all claims to console:
      [AuthDebug] Claim: role = Admin
      [AuthDebug] Claim: permission = CanManageUsers
      [AuthDebug] Claim: permission = CanManageCustomers
      ...
         ?
         ?
5. Check Authorization Policy
   ?? Endpoint: [Authorize(Policy = "AdminOnly")]
   ?? Policy Definition: RequireClaim("role", "Admin")
   ?? Check: Does user have claim with 
   ?         type="role" and value="Admin"?
   ?? Found in claims: role = "Admin" ?
   ?? Policy satisfied ? Continue to controller
         ?
         ?
6. Execute Controller Action
   ?? AdminController.Dashboard()
   ?? Query data from database
   ?? Return 200 OK with data
   ?? Response sent to client
```

---

## 4. Policy Matching Examples

### Example 1: AdminOnly Policy

**Policy Definition:**
```csharp
o.AddPolicy("AdminOnly", p => 
    p.RequireClaim("role", "Admin"));
```

**Endpoint:**
```csharp
[Authorize(Policy = "AdminOnly")]
public IActionResult Dashboard() { ... }
```

**JWT Claims:**
```json
{
  "role": "Admin",
  "permission": [...],
  ...
}
```

**Match Result:**
- Policy requires: claim where type="role" and value="Admin"
- JWT has: role="Admin"
- ? MATCH ? Access granted

---

### Example 2: CanManageUsers Policy

**Policy Definition:**
```csharp
o.AddPolicy("CanManageUsers", p => 
    p.RequireClaim("permission", "CanManageUsers"));
```

**Endpoint:**
```csharp
[Authorize(Policy = "CanManageUsers")]
public IActionResult GetUsers() { ... }
```

**JWT Claims:**
```json
{
  "permission": "CanManageUsers",
  "permission": "CanManageCustomers",
  "permission": "CanConfigurePackages",
  ...
}
```

**Match Result:**
- Policy requires: claim where type="permission" and value="CanManageUsers"
- JWT has: permission="CanManageUsers" ?
- ? MATCH ? Access granted

---

### Example 3: Failed Authorization

**Policy Definition:**
```csharp
o.AddPolicy("CanApproveTransfer", p => 
    p.RequireClaim("permission", "CanApproveTransfer"));
```

**Endpoint:**
```csharp
[Authorize(Policy = "CanApproveTransfer")]
public IActionResult ApproveTransfer(Guid id) { ... }
```

**JWT Claims (Admin):**
```json
{
  "role": "Admin",
  "permission": "CanManageUsers",
  "permission": "CanManageCustomers",
  ...
  // ? NO "CanApproveTransfer"
}
```

**Match Result:**
- Policy requires: claim where type="permission" and value="CanApproveTransfer"
- JWT has: No such claim ?
- ? NO MATCH ? 403 Forbidden

---

## 5. Database to JWT Flow

```
????????????????????????????????????????
?  AspNetRoleClaims Table              ?
?  ?? RoleId: Admin                    ?
?  ?? ClaimType: "permission"          ?
?  ?? ClaimValue: "CanManageUsers"     ?
?  ?                                  ?
?  ?? RoleId: Admin                    ?
?  ?? ClaimType: "permission"          ?
?  ?? ClaimValue: "CanManageCustomers" ?
????????????????????????????????????????
         ?
         ? RoleManager.GetClaimsAsync()
         ?
         ?
????????????????????????????????????????
?  JwtService.GenerateToken()          ?
?  ?? For each permission claim        ?
?  ?? Add to JWT                       ?
?  ?? Result: Multiple permission      ?
?            claims in token           ?
????????????????????????????????????????
         ?
         ?
         ?
????????????????????????????????????????
?  JWT Token Payload                   ?
?  {                                   ?
?    "role": "Admin",                  ?
?    "permission": [                   ?
?      "CanManageUsers",               ?
?      "CanManageCustomers",           ?
?      ...                             ?
?    ]                                 ?
?  }                                   ?
????????????????????????????????????????
         ?
         ?
         ?
????????????????????????????????????????
?  Authorization Policy Check          ?
?  ?? Check: Has permission claim?     ?
?  ?? Found: YES ?                    ?
?  ?? Result: Access granted           ?
????????????????????????????????????????
```

---

## 6. Key Classes & Methods

### JwtService.cs

```csharp
public async Task<string> GenerateStaffTokenAsync(User user)
{
    // 1. Get user's roles
    var roleNames = await _userManager.GetRolesAsync(user);
    
    // 2. For each role
    foreach (var roleName in roleNames)
    {
        var roleEntity = await _roleManager.FindByNameAsync(roleName);
        
        // 3. Get role's permission claims
        var roleClaims = await _roleManager.GetClaimsAsync(roleEntity);
        
        // 4. Add each permission to JWT claims
        foreach (var claim in roleClaims)
        {
            claims.Add(new Claim("permission", claim.Value));
        }
    }
    
    // 5. Build and sign token
    return BuildToken(claims);
}
```

### AuthController.cs

```csharp
[HttpPost("staff/login")]
public async Task<IActionResult> StaffLogin([FromBody] StaffLoginRequest req)
{
    // 1. Validate credentials
    var user = await _userManager.FindByEmailAsync(req.Email);
    
    // 2. Generate JWT with permission claims
    var token = await _jwt.GenerateStaffTokenAsync(user);
    
    // 3. For login response, also fetch permissions
    var roleEntity = await _roleManager.FindByNameAsync(roleName);
    var roleClaims = await _roleManager.GetClaimsAsync(roleEntity);
    var permissionList = roleClaims
        .Where(c => c.Type == "permission")
        .Select(c => c.Value)
        .ToList();
    
    // 4. Return token + permissions
    return Ok(new { token, user = new { ..., permissions } });
}
```

### Program.cs

```csharp
// Define policies that check for permission claims
builder.Services.AddAuthorization(o =>
{
    // Policy checks for "permission" claim with specific value
    o.AddPolicy("CanManageUsers", p => 
        p.RequireClaim("permission", "CanManageUsers"));
    
    o.AddPolicy("CanManageCustomers", p => 
        p.RequireClaim("permission", "CanManageCustomers"));
    
    // ... more policies
});
```

---

## 7. Request-Response Flow Diagram

```
CLIENT                          SERVER
  ?                               ?
  ?? Login Request ???????????????>
  ?  email: admin@ddfc.com.pk      ?
  ?  password: Admin@2026!         ?
  ?                                ?? Validate Credentials
  ?                                ?? Get User Roles
  ?                                ?? Get Role Permissions
  ?                                ?? Build JWT with Claims
  ?                                ?
  ? <????? Login Response ??????????
  ?  token: eyJ0eXA...            ?
  ?  user: {..., permissions:[]}  ?
  ?                                ?
  ? (Store token in localStorage) ?
  ?                                ?
  ? Protected Endpoint Request ???>
  ?  GET /api/v1/admin/dashboard  ?
  ?  Header: Authorization: Bearer ?
  ?                                ?? Extract JWT
  ?                                ?? Validate Signature
  ?                                ?? Parse Claims
  ?                                ?? Check Policy (AdminOnly)
  ?                                ?? Find: role="Admin" ?
  ?                                ?? Execute Controller
  ?                                ?
  ? <?????? 200 OK Response ?????????
  ?  {dashboard data}              ?
  ?                                ?
```

---

## 8. Complete Admin User Example

### Database State
```sql
-- Admin Role
SELECT * FROM AspNetRoles WHERE Name = 'Admin'
-- Result: Id={guid}, Name='Admin', IsBuiltIn=1

-- Admin Role Permissions
SELECT * FROM AspNetRoleClaims 
WHERE RoleId = {Admin Role ID} AND ClaimType = 'permission'
-- Result: 10 rows
--   1. CanManageUsers
--   2. CanManageCustomers
--   3. CanConfigurePackages
--   4. CanViewAllRequests
--   5. CanViewReports
--   6. CanManageTemplates
--   7. CanManageDepartments
--   8. CanManageRoles
--   9. CanManageSettings
--   10. CanResetRoundRobin

-- Admin User
SELECT * FROM AspNetUsers WHERE Email = 'admin@ddfc.com.pk'
-- Result: Id={guid}, Email, FullName, IsActive=1, IsDeleted=0

-- User to Role Assignment
SELECT * FROM AspNetUserRoles 
WHERE UserId = {Admin User ID}
-- Result: 1 row (Admin User -> Admin Role)
```

### JWT Token Generated
```json
{
  "typ": "JWT",
  "alg": "HS256"
}
.
{
  "sub": "user-guid",
  "email": "admin@ddfc.com.pk",
  "name": "System Administrator",
  "role": "Admin",
  "departmentId": "dept-guid",
  "userType": "staff",
  "permission": "CanManageUsers",
  "permission": "CanManageCustomers",
  "permission": "CanConfigurePackages",
  "permission": "CanViewAllRequests",
  "permission": "CanViewReports",
  "permission": "CanManageTemplates",
  "permission": "CanManageDepartments",
  "permission": "CanManageRoles",
  "permission": "CanManageSettings",
  "permission": "CanResetRoundRobin",
  "iat": 1234567890,
  "exp": 1234591890
}
.
{signature}
```

### Policy Checks Pass
```
Endpoint: GET /api/v1/admin/dashboard
Policy: [Authorize(Policy = "AdminOnly")]

Check 1: RequireClaim("role", "Admin")
  - JWT has: role = "Admin" ? PASS

Check 2: (if another endpoint had CanManageUsers policy)
  - JWT has: permission = "CanManageUsers" ? PASS

Result: 200 OK - Access granted
```

---

## 9. Common Issues & Solutions

| Issue | Cause | Solution |
|-------|-------|----------|
| 403 Forbidden | Missing permission claim | Check JWT has required permission |
| 401 Unauthorized | Invalid JWT signature | Get fresh token |
| Missing permissions | Role claims not in DB | Reseed database |
| Claim type mismatch | Wrong claim type used | Use "permission" for permissions |
| Multiple claims | ASP.NET feature | System supports multiple claims with same name |

---

## 10. Verification Commands

### Decode JWT Token
```bash
# Use jwt.io or jq
# jwt.io: Paste token in Debugger
# jq: echo $token | jq .[1] | base64 -d
```

### Check Database
```sql
-- Verify role has permissions
SELECT ClaimType, ClaimValue FROM AspNetRoleClaims 
WHERE RoleId = (SELECT Id FROM AspNetRoles WHERE Name = 'Admin')

-- Verify user has role
SELECT * FROM AspNetUserRoles 
WHERE UserId = (SELECT Id FROM AspNetUsers WHERE Email = 'admin@ddfc.com.pk')
```

### Check Logs
```
Look for: [AuthDebug] Claim: permission = CanManageUsers
```

---

**Complete Technical Flow Documentation** ?
