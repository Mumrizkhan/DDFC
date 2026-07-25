# Claims-Based Authorization Quick Reference

## ?? Complete Policy List

The DDFC API now has **70+ authorization policies** configured! For the complete reference, see:
- **?? [AUTHORIZATION_POLICIES.md](./AUTHORIZATION_POLICIES.md)** - Complete policy reference with examples

This guide provides quick examples for using claims in your code.

---

## Setting Up Policies in Program.cs

? **Already configured!** All 70+ policies are set up in `Program.cs`.

See `AUTHORIZATION_POLICIES.md` for the complete list including:
- ? **4** Basic user type policies
- ? **11** Role-based policies
- ? **27** Permission-based policies (RECOMMENDED)
- ? **7** User claim-based policies
- ? **12** Department-based policies
- ? **10** Combined/complex policies

---

## Quick Examples

### Using Permission Policies (RECOMMENDED)

```csharp
// Transfer approval
[Authorize(Policy = "CanApproveTransfer")]
[HttpPost("approve-transfer")]
public async Task<IActionResult> ApproveTransfer([FromBody] ApprovalDto dto)
{
    // Only users with "CanApproveTransfer" permission can access
}

// Upload architectural plan
[Authorize(Policy = "CanUploadPlan")]
[HttpPost("architectural-plans")]
public async Task<IActionResult> UploadPlan([FromBody] PlanDto dto)
{
    // Only users with "CanUploadPlan" permission can access
}

// Admin only
[Authorize(Policy = "CanManageUsers")]
[HttpPost("users")]
public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
{
    // Only users with "CanManageUsers" permission can access
}
```

### Multiple Policies (AND)

```csharp
[Authorize(Policy = "CanCompleteStructure")]
[Authorize(Policy = "HasEngineeringLicense")]
[HttpPost("complete-structural-review")]
public async Task<IActionResult> CompleteStructuralReview([FromBody] ReviewDto dto)
{
    // Only licensed structure engineers can access
}
```

### Conditional Authorization

```csharp
[Authorize(Policy = "StaffOrAdmin")]
[HttpGet("requests/{id}")]
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

---

## Accessing Claims in Code

### Get Claim Value

```csharp
public class MyService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public MyService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public async Task DoSomething()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        
        // Get department code
        var deptCode = user?.FindFirst("departmentCode")?.Value;
        
        // Get user type
        var userType = user?.FindFirst("userType")?.Value;
        
        // Get role
        var role = user?.FindFirst("role")?.Value;
        
        // Get specialization
        var specialty = user?.FindFirst("designSpecialty")?.Value;
        
        // Get years of experience
        var yearsExp = user?.FindFirst("yearsOfExperience")?.Value;
        if (int.TryParse(yearsExp, out int years))
        {
            // Use years value
        }
    }
}
```

### Check if Claim Exists

```csharp
// Check if user has any approval authority
var hasApprovalAuth = User.HasClaim(c => c.Type == "approvalAuthority");

// Check specific approval authority
var isTransferApprover = User.HasClaim("approvalAuthority", "Transfer");

// Check if user has engineering license
var isLicensed = User.HasClaim(c => c.Type == "engineeringLicense");
```

### Get All Claims of a Type

```csharp
// Get all permission claims
var permissions = User.Claims
    .Where(c => c.Type == "permission")
    .Select(c => c.Value)
    .ToList();

// Check if user has any of the permissions
if (permissions.Any(p => new[] { "CanUploadPlan", "CanCompleteStructure" }.Contains(p)))
{
    // User is a designer
}
```

## Authorization Requirements

### Custom Requirement

```csharp
public class MinimumExperienceRequirement : IAuthorizationRequirement
{
    public int MinimumYears { get; }
    
    public MinimumExperienceRequirement(int minimumYears)
    {
        MinimumYears = minimumYears;
    }
}

public class MinimumExperienceHandler : AuthorizationHandler<MinimumExperienceRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MinimumExperienceRequirement requirement)
    {
        var yearsExp = context.User.FindFirst("yearsOfExperience")?.Value;
        
        if (int.TryParse(yearsExp, out int years) && years >= requirement.MinimumYears)
        {
            context.Succeed(requirement);
        }
        
        return Task.CompletedTask;
    }
}

// Register in Program.cs
builder.Services.AddSingleton<IAuthorizationHandler, MinimumExperienceHandler>();

builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("ExperiencedArchitect", p => 
        p.Requirements.Add(new MinimumExperienceRequirement(5)));
});
```

## Common Patterns

### Department-Based Access

```csharp
public async Task<IActionResult> GetDepartmentData()
{
    var deptCode = User.FindFirst("departmentCode")?.Value;
    
    return deptCode switch
    {
        "AD" => await GetArchitectureData(),
        "SD" => await GetStructureData(),
        "MD" => await GetMEPData(),
        _ => Forbid()
    };
}
```

### Role Hierarchy

```csharp
public bool CanApprove()
{
    // Design Head > Principal Architect > Architect
    if (User.HasClaim("permission", "CanFinalApprove"))
        return true;
    
    if (User.HasClaim("permission", "CanPrincipalApprove"))
        return true;
    
    if (User.HasClaim("permission", "CanUploadPlan"))
        return true;
    
    return false;
}
```

### Specialty-Based Routing

```csharp
public async Task<IActionResult> AssignTask(Guid taskId)
{
    var specialty = User.FindFirst("designSpecialty")?.Value;
    
    if (specialty == "Residential")
    {
        return await AssignResidentialTask(taskId);
    }
    else if (specialty == "Commercial")
    {
        return await AssignCommercialTask(taskId);
    }
    
    return BadRequest("Unknown specialty");
}
```

## Testing Claims

### Create Test User with Claims

```csharp
var claims = new List<Claim>
{
    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
    new Claim(ClaimTypes.Email, "test@ddfc.com.pk"),
    new Claim("role", "Architect"),
    new Claim("userType", "staff"),
    new Claim("departmentCode", "AD"),
    new Claim("permission", "CanUploadPlan"),
    new Claim("designSpecialty", "Residential"),
    new Claim("yearsOfExperience", "5")
};

var identity = new ClaimsIdentity(claims, "TestAuth");
var principal = new ClaimsPrincipal(identity);
```

### Mock User in Tests

```csharp
[Fact]
public async Task OnlyArchitectsCanUploadPlans()
{
    // Arrange
    var user = CreateTestUser(role: "Architect", permission: "CanUploadPlan");
    _controller.ControllerContext = new ControllerContext
    {
        HttpContext = new DefaultHttpContext { User = user }
    };
    
    // Act
    var result = await _controller.UploadPlan(planDto);
    
    // Assert
    Assert.IsType<OkResult>(result);
}
```

## Summary

- ? **Role Claims**: Permissions assigned to roles (all users in that role get them)
- ? **User Claims**: Metadata specific to individual users
- ? **Policies**: Declarative authorization rules
- ? **Requirements**: Custom authorization logic
- ? **Claims in JWT**: Automatically included in authentication token

Use **role claims** for permissions and **user claims** for user-specific metadata and attributes!
