# SQL Cleanup Scripts Summary

## Overview
Three comprehensive SQL cleanup scripts have been created to remove all data from the DDFC and Workflow databases. Choose the one that best fits your needs.

## Files Created

| File | Purpose | Best For |
|------|---------|----------|
| `SQL_CLEANUP_SCRIPT.sql` | Basic, straightforward cleanup | Development environments |
| `SQL_CLEANUP_SCRIPT_WITH_TRANSACTION.sql` | Safe cleanup with rollback | Production environments |
| `SQL_CLEANUP_SCENARIOS.sql` | Individual cleanup snippets | Targeted, specific cleanups |
| `SQL_CLEANUP_README.md` | Complete usage documentation | Reference & troubleshooting |

## Quick Start

### Fastest Setup (Development)
1. Open `SQL_CLEANUP_SCRIPT.sql`
2. Replace `[YourDatabaseName]` with your database name
3. Run (F5)
4. Done! Database is now empty

### Safest Setup (Production)
1. Open `SQL_CLEANUP_SCRIPT_WITH_TRANSACTION.sql`
2. Replace `[YourDatabaseName]` with your database name
3. Run (F5)
4. Check output for success or errors
5. If errors occur, transaction automatically rolls back

### Targeted Cleanup
1. Open `SQL_CLEANUP_SCENARIOS.sql`
2. Find your scenario (e.g., "Delete Only Users")
3. Run just that section
4. Done!

## What Gets Deleted

### ? DDFC Schema
- Customers (all)
- Plots (all)
- Packages & Line Items (all)
- Possession Requests (all)
- Payments & Challans (all)
- Appointments (all)
- Task Assignments (all)
- Support Tickets & Replies (all)
- All workflow history
- All documents & reports
- All technical files
- All departments

### ? Identity Schema
- Users (all)
- Roles (all)
- User/Role Claims (all)
- User Logins/Tokens (all)

### ? Workflow Schema
- Processes (all)
- Process Steps (all)
- Workflow Requests (all)
- Request Steps & Actions (all)
- Step Transitions (all)

### ? NOT Deleted
- Table schemas
- Database structure
- Indexes
- Views
- Migration history
- Database integrity

## Key Features

### SQL_CLEANUP_SCRIPT.sql
? Simple execution  
? Fast completion  
? Suitable for dev environments  
? No rollback capability  
? No error recovery  

### SQL_CLEANUP_SCRIPT_WITH_TRANSACTION.sql
? Transaction wrapping  
? Automatic rollback on error  
? Detailed progress messages  
? Error logging  
? Production-ready  
? Safe to run multiple times  

### SQL_CLEANUP_SCENARIOS.sql
? Delete specific data types  
? Don't affect other data  
? Useful for targeted cleanup  
? Good for testing  
? Easy to modify  

## Step-by-Step Usage

### For SQL_CLEANUP_SCRIPT.sql

```sql
1. Open file in SSMS
2. Find line: USE [YourDatabaseName];
3. Replace with: USE [DDFC];
4. Select All (Ctrl+A)
5. Execute (F5)
6. Review output
7. Done!
```

### For SQL_CLEANUP_SCRIPT_WITH_TRANSACTION.sql

```sql
1. Open file in SSMS
2. Find line: USE [YourDatabaseName];
3. Replace with: USE [DDFC];
4. Select All (Ctrl+A)
5. Execute (F5)
6. Wait for completion
7. Check "? CLEANUP COMPLETED SUCCESSFULLY!" message
8. Review verification counts (all should be 0)
9. Done!
```

### For SQL_CLEANUP_SCENARIOS.sql

```sql
1. Open file in SSMS
2. Find scenario you need (e.g., SCENARIO 2: Delete Possession Requests)
3. Replace [YourDatabaseName] with your database name
4. Select only that scenario's code
5. Execute (F5)
6. Done!
```

## Safety Checklist

- [ ] Backed up database before running
- [ ] Using correct database name
- [ ] Have database admin privileges
- [ ] Running against development/test database first
- [ ] Reviewed the script before execution
- [ ] Have access to restore from backup if needed
- [ ] Aware that this is irreversible (without backup)

## After Cleanup

Once cleanup is complete:

1. **Verify**: Check that record counts show 0 for all tables
2. **Application Start**: Application will call seeders on startup
3. **Manual Seeding**: Or run seeder methods manually
4. **Custom Data**: Or populate with your own data

## Troubleshooting

### Issue: "Could not find stored procedure 'sp_MSForEachTable'"
- This is a SQL Server system procedure
- May not exist in SQL Server Express Edition
- Run as admin or contact DBA

### Issue: "The specified schema name 'ddfc' either does not exist or you do not have permission"
- Check database name is correct
- Verify you have permissions
- Run as admin account

### Issue: "The DELETE statement conflicted with a FOREIGN KEY constraint"
- Ensure constraint disabling ran successfully
- Try using TRANSACTION version (has better error handling)
- Run CONSTRAINT disabling manually

### Issue: Database still has data after cleanup
- Check you didn't accidentally run partial script
- Verify all DELETE statements executed
- Try running cleanup again

## Performance Expectations

| Script | Time | Notes |
|--------|------|-------|
| Basic Cleanup | 5-10 seconds | Fast, no overhead |
| Transaction Cleanup | 10-20 seconds | Slightly slower due to logging |
| Scenarios | 1-5 seconds | Depends on scenario |

Times depend on:
- Database size
- SQL Server performance
- Network latency
- System load

## Common Mistakes to Avoid

? **Not backing up first** - Always backup before cleanup
? **Wrong database name** - Double-check the database name
? **Running on production without transaction** - Use TRANSACTION version
? **Not reading error messages** - Error messages tell you what went wrong
? **Running partial script** - Execute entire script at once
? **Assuming old constraint state** - Script re-enables all constraints

## Integration with .NET Application

The cleanup works perfectly with the existing .NET cleanup code:

```
SQL Cleanup (removes all data)
          ?
Application Startup
          ?
DatabaseCleanup.ClearAllDataAsync() (C# cleanup)
          ?
DDFCDataSeeder.SeedAsync() (runs seeders if implemented)
          ?
Database Ready!
```

You can use either:
- **SQL Scripts**: For direct database control
- **.NET Cleanup**: Runs automatically on startup
- **Both**: SQL before deployment, .NET on app startup

## Comparison Matrix

| Feature | Basic SQL | Transaction SQL | .NET Code |
|---------|-----------|-----------------|-----------|
| No backup needed | ? | ? | ? |
| Automatic rollback | ? | ? | ? |
| Transaction support | ? | ? | ? |
| Error messages | Basic | Detailed | C# logging |
| Production ready | ? | ? | ? |
| Runs on startup | ? | ? | ? |
| Manual control | ? | ? | ? |

## Next Steps

1. **Choose script** based on your environment
2. **Backup database** for safety
3. **Modify database name** in script
4. **Execute script** in SQL Server Management Studio
5. **Verify results** with verification queries
6. **Restart application** to trigger seeders

## Support

For issues:
1. Check `SQL_CLEANUP_README.md` for detailed troubleshooting
2. Review error messages in output
3. Check SQL Server error logs
4. Verify permissions and database name
5. Try TRANSACTION version for better error handling

## Related Documentation

- `DATABASE_CLEANUP_SUMMARY.md` - .NET cleanup functionality
- `src\DDFC.Infrastructure\Data\DatabaseCleanup.cs` - .NET cleanup code
- `src\DDFC.Infrastructure\Data\DDFCDataSeeder.cs` - Seeder implementation

---

**Last Updated**: 2024
**Status**: Production Ready
**Compatibility**: SQL Server 2019+
