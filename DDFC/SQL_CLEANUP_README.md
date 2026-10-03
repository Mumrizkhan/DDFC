# SQL Database Cleanup Scripts

This directory contains SQL scripts to clean up all lookups and data from the DDFC database.

## Files Included

### 1. `SQL_CLEANUP_SCRIPT.sql` (Basic Cleanup)
Simple, straightforward cleanup script without transaction support.

**Use when:**
- You want a quick cleanup
- You're confident in the cleanup process
- You don't need rollback capability

**Features:**
- Disables all foreign key constraints
- Deletes all data from both DDFC and Workflow databases
- Re-enables all constraints
- Resets identity seeds for clean auto-increment IDs
- Includes verification queries

### 2. `SQL_CLEANUP_SCRIPT_WITH_TRANSACTION.sql` (Advanced Cleanup)
Production-grade cleanup script with transaction support and error handling.

**Use when:**
- You want safe, rollback-capable cleanup
- You need error handling and detailed logging
- You're in a production or critical environment
- You want detailed progress reporting

**Features:**
- Wraps all operations in a transaction
- Automatic rollback on any error
- Detailed progress messages
- Error handling with specific error information
- Disables/re-enables constraints safely
- Resets identity seeds
- Verification queries at the end

## Prerequisites

- **SQL Server Management Studio (SSMS)** or similar SQL Server query tool
- **Database Admin Rights** - you need sufficient permissions to:
  - Alter table constraints
  - Delete data from all tables
  - Execute sp_MSForEachTable
  - Use DBCC commands

## How to Use

### Option 1: Basic Cleanup (Recommended for Development)

1. Open `SQL_CLEANUP_SCRIPT.sql` in SQL Server Management Studio
2. **IMPORTANT:** Replace `[YourDatabaseName]` with your actual database name
   - Look for: `USE [YourDatabaseName];`
   - Change to: `USE [DDFC];` (or your actual database name)
3. Select all the script content (Ctrl+A)
4. Execute (F5 or click Execute)
5. Review the output to verify cleanup was successful

### Option 2: Advanced Cleanup with Rollback (Recommended for Production)

1. Open `SQL_CLEANUP_SCRIPT_WITH_TRANSACTION.sql` in SQL Server Management Studio
2. **IMPORTANT:** Replace `[YourDatabaseName]` with your actual database name
   - Look for: `USE [YourDatabaseName];`
   - Change to: `USE [DDFC];` (or your actual database name)
3. Select all the script content (Ctrl+A)
4. Execute (F5 or click Execute)
5. If any errors occur, the transaction automatically rolls back
6. Review the output for success/failure status

## Data Deleted

The scripts will delete the following data:

### DDFC Schema (ddfc)
- All customer records
- All plot records
- All packages and package line items
- All possession requests
- All payments and challans
- All appointments
- All task assignments
- All support tickets and replies
- All workflow history records
- All CAD files and assignments
- All architectural plans
- All technical reports (MEP, Structural, Soil Test)
- All survey forms and observations
- All undertakings
- All possession certificates
- All documents
- All templates
- All departments

### Identity Schema (identity)
- All users
- All roles
- All user claims
- All role claims
- All user logins
- All user tokens
- All user roles

### Workflow Schema (workflow)
- All processes
- All process steps
- All step actions
- All step transitions
- All workflow requests
- All request steps
- All request actions

## What is NOT Deleted

- **Database Schema**: Table definitions remain intact
- **Migrations**: EF Core migration history is preserved
- **Indexes**: All database indexes remain
- **Views**: Any database views remain intact

## After Cleanup

Once the script completes successfully:

1. ? The database is completely empty of all transactional data
2. ? All lookup tables are cleared
3. ? All user and role data is removed
4. ? All workflow data is cleared
5. ? The database is ready for fresh seeding

You can then:
- Run the .NET application startup which will call the seeders
- OR manually run the `DDFCDataSeeder.SeedAsync()` method
- OR populate the database with custom seed data

## Verification

Both scripts include verification queries that will show record counts for key tables:

```
Customers
Plots
Packages
Departments
PossessionRequests
Payments
Appointments
TaskAssignments
SupportTickets
Users
Roles
WorkflowProcesses
WorkflowRequests
```

All should show 0 records after cleanup.

## Error Recovery

### If using basic script (SQL_CLEANUP_SCRIPT.sql)
- If an error occurs mid-cleanup, you may have inconsistent data
- You may need to manually fix foreign key constraints
- Consider using the transaction-based script instead

### If using transaction script (SQL_CLEANUP_SCRIPT_WITH_TRANSACTION.sql)
- If an error occurs, the entire transaction is automatically rolled back
- No manual cleanup needed
- Run the script again to retry

## Troubleshooting

### Error: "Could not find stored procedure 'sp_MSForEachTable'"
- This is a SQL Server system procedure
- Ensure you're running against SQL Server (not Express Edition limited versions)
- Contact your DBA if this doesn't exist in your environment

### Error: "ALTER TABLE ... NOCHECK CONSTRAINT ALL failed"
- You may not have sufficient permissions
- Contact your database administrator
- Run the script with admin credentials

### Error: "User 'xxx' does not have permission to delete"
- Your login doesn't have permission to delete data
- Ask your DBA to grant DELETE and ALTER TABLE permissions
- Use a SQL Server account with higher privileges

### Foreign Key Constraint Errors
- The script disables constraints before deletion
- If you see FK errors, one of the constraint disable/enable steps failed
- Run the constraint disable command manually:
  ```sql
  EXEC sp_MSForEachTable @command1='ALTER TABLE ? NOCHECK CONSTRAINT ALL'
  ```

## Command Line Usage (Optional)

You can also run these scripts from command line using sqlcmd:

```bash
sqlcmd -S ServerName -d DatabaseName -U Username -P Password -i SQL_CLEANUP_SCRIPT.sql
```

Or using PowerShell:

```powershell
Invoke-Sqlcmd -ServerInstance "ServerName" -Database "DatabaseName" -InputFile "SQL_CLEANUP_SCRIPT.sql"
```

## Safety Recommendations

1. **Backup First**: Always backup your database before running cleanup scripts
   ```sql
   BACKUP DATABASE [YourDatabaseName] TO DISK = 'C:\Backups\DDFC_Backup.bak'
   ```

2. **Test in Dev First**: Run on development database before production

3. **Use Transaction Version**: Use the transaction-based script in production

4. **Review the Script**: Always review the script before running

5. **Keep a Backup**: Keep a backup until you confirm the cleanup worked

## Related Files

- `DDFCDataSeeder.cs` - .NET code that populates initial data
- `DatabaseCleanup.cs` - .NET code that cleans up data
- `DATABASE_CLEANUP_SUMMARY.md` - Summary of cleanup functionality

## Support

If you encounter issues:
1. Check the Troubleshooting section above
2. Review error messages carefully
3. Verify database name and permissions
4. Check SQL Server error logs
5. Contact your database administrator
