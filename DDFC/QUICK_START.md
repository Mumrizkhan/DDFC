# ?? DDFC API Quick Start Guide

## Prerequisites
- ? Visual Studio 2022 (with ASP.NET workload) OR Visual Studio Code
- ? .NET 9 SDK
- ? SQL Server (LocalDB, Express, or Full)
- ? IIS Express (comes with Visual Studio)

## Running the API

### ?? Visual Studio 2022 (Recommended)

1. **Open the solution**
   - Double-click `DDFC.sln`

2. **Select launch profile**
   - At the top toolbar, find the dropdown next to the green play button
   - Select **"IIS Express"**

3. **Run the application**
   - Press `F5` (Debug) or `Ctrl+F5` (Run without debugging)
   - Browser will automatically open to Swagger UI at `https://localhost:44321/swagger`

### ?? Visual Studio Code

1. **Open the workspace**
   ```bash
   code .
   ```

2. **Select debug configuration**
   - Press `F5` or go to Run and Debug (Ctrl+Shift+D)
   - Select **"Launch API (IIS Express)"** from the dropdown

3. **Start debugging**
   - Press `F5`
   - Browser will open to Swagger UI

### ?? Command Line

From the project root:

```bash
# Navigate to API project
cd src\DDFC.API

# Run with IIS Express profile
dotnet run --launch-profile "IIS Express"

# OR run with Kestrel HTTPS
dotnet run --launch-profile https

# OR run with Kestrel HTTP only
dotnet run --launch-profile http
```

## ?? Access URLs

Once running, access the API at:

| Profile | HTTP | HTTPS | Swagger |
|---------|------|-------|---------|
| **IIS Express** | http://localhost:21547 | https://localhost:44321 | https://localhost:44321/swagger |
| **Kestrel HTTPS** | http://localhost:5054 | https://localhost:7016 | https://localhost:7016/swagger |
| **Kestrel HTTP** | http://localhost:5054 | - | http://localhost:5054/swagger |

## ??? Database Setup

The database is **automatically configured** on first run:

1. **Migrations Applied** ?
   - DDFC schema (identity, business entities)
   - Workflow schema (workflow engine)

2. **Data Seeded** ?
   - 12 Departments
   - 12 Roles with Role Claims
   - 25 Users with User Claims
   - Sample Packages

### Manual Database Commands

If you need to manually manage the database:

```bash
# Apply DDFC migrations
dotnet ef database update --context DDFCDbContext --project src\DDFC.Infrastructure --startup-project src\DDFC.API

# Apply Workflow migrations
dotnet ef database update --context WorkflowDbContext --project src\DDFC.Infrastructure --startup-project src\DDFC.API

# Drop database (WARNING: Deletes all data)
dotnet ef database drop --context DDFCDbContext --project src\DDFC.Infrastructure --startup-project src\DDFC.API
```

## ?? Test Authentication

### Get JWT Token

Use Swagger UI or any HTTP client:

**Endpoint:** `POST /api/v1/auth/staff/login`

**Request Body:**
```json
{
  "email": "admin@ddfc.com.pk",
  "password": "Admin@2026!"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": "...",
    "email": "admin@ddfc.com.pk",
    "fullName": "System Administrator",
    "role": "Admin"
  }
}
```

### Use Token in Requests

In Swagger UI:
1. Click the **"Authorize"** button (lock icon)
2. Enter: `Bearer {your-token}`
3. Click **"Authorize"**
4. Now you can test protected endpoints

In Postman/curl:
```bash
curl -H "Authorization: Bearer {your-token}" https://localhost:44321/api/v1/packages
```

## ?? Seeded Test Users

| Email | Password | Role | Department |
|-------|----------|------|------------|
| admin@ddfc.com.pk | Admin@2026! | Admin | Administration |
| ali.ahmed@ddfc.com.pk | Reception@2026! | Reception Officer | Front Desk |
| hassan.raza@ddfc.com.pk | Transfer@2026! | Transfer Officer | Transfer Branch |
| imran.siddiqui@ddfc.com.pk | Finance@2026! | Finance Officer | Finance Branch |
| kamran.ali@ddfc.com.pk | TownPlan@2026! | Town Planner | Town Planning |
| bilal.ahmed@ddfc.com.pk | BuildControl@2026! | Building Control Officer | Building Control |
| usman.tariq@ddfc.com.pk | Architect@2026! | Architect | Architecture Dept |
| fahad.mirza@ddfc.com.pk | Structure@2026! | Structure Engineer | Structure Dept |
| tariq.mahmood@ddfc.com.pk | MEP@2026! | MEP Engineer | MEP Dept |
| jawad.abbas@ddfc.com.pk | Principal@2026! | Principal Architect | Principal Office |
| zulfiqar.ali@ddfc.com.pk | DesignHead@2026! | DHA Design Head | DHA Design Dept |
| support@ddfc.com.pk | Support@2026! | Technical Support | Technical Support |

**See `docs\IDENTITY_SEEDING.md` for all 25 users**

## ?? Test Endpoints

### Public Endpoints (No Auth Required)
```
POST /api/v1/auth/staff/login
POST /api/v1/auth/customer/login
POST /api/v1/auth/customer/verify-otp
```

### Protected Endpoints (Auth Required)

**Packages:**
```
GET  /api/v1/packages
GET  /api/v1/packages/{id}
POST /api/v1/packages (Admin only)
PUT  /api/v1/packages/{id} (Admin only)
```

**Possession Requests:**
```
GET  /api/v1/possession-requests
POST /api/v1/possession-requests
GET  /api/v1/possession-requests/{id}
PUT  /api/v1/possession-requests/{id}/status
```

**Admin:**
```
GET  /api/v1/admin/users
POST /api/v1/admin/users
GET  /api/v1/admin/roles
POST /api/v1/admin/roles
```

## ?? Troubleshooting

### Port Already in Use
Change ports in `src\DDFC.API\Properties\launchSettings.json`:
```json
"iisExpress": {
  "applicationUrl": "http://localhost:YOUR_PORT",
  "sslPort": YOUR_SSL_PORT
}
```

### SSL Certificate Issues
Trust the development certificate:
```bash
dotnet dev-certs https --trust
```

### Database Connection Failed
1. Check SQL Server is running
2. Verify connection string in `appsettings.json`
3. Try using LocalDB: `Server=(localdb)\\mssqllocaldb;...`

### Migrations Not Applied
Delete the database and restart the app:
```bash
dotnet ef database drop --context DDFCDbContext
# Restart the application - migrations will apply automatically
```

### Seeding Errors
Clear all data and restart:
```sql
-- Run in SQL Server Management Studio
DELETE FROM identity.UserRoles;
DELETE FROM identity.UserClaims;
DELETE FROM identity.RoleClaims;
DELETE FROM identity.Users;
DELETE FROM identity.Roles;
DELETE FROM ddfc.Departments;
-- Restart application
```

## ?? Documentation

- **Identity & Seeding:** `docs\IDENTITY_SEEDING.md`
- **Authorization Guide:** `docs\CLAIMS_AUTHORIZATION_GUIDE.md`
- **IIS Express Setup:** `docs\IIS_EXPRESS_SETUP.md`
- **Workflow Engine:** `WorkflowEngine\README.md` (if available)

## ?? Next Steps

1. ? **API is running** in IIS Express
2. ? **Swagger UI** available for testing
3. ? **Database seeded** with test data
4. ? **Authentication** working with JWT

**Start developing your features!** ??

### Useful Commands

```bash
# Watch for changes and auto-reload
dotnet watch run --project src\DDFC.API

# Run tests
dotnet test

# Check for outdated packages
dotnet list package --outdated

# Update all packages
dotnet tool update --global dotnet-ef
```

## ?? Need Help?

1. Check the documentation in `docs\` folder
2. Review the error messages in console/output window
3. Check SQL Server logs for database issues
4. Review `appsettings.Development.json` for configuration

**Happy coding!** ???
