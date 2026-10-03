# Database Cleanup Summary

## Overview
All lookup data and existing records have been successfully removed from both the DDFC database and WorkflowEngine database on application startup.

## Changes Made

### 1. Created `DatabaseCleanup.cs`
- **File**: `src\DDFC.Infrastructure\Data\DatabaseCleanup.cs`
- **Purpose**: Comprehensive database cleanup utility that removes all data from both databases
- **Features**:
  - Removes all DDFC database records (users, roles, departments, plots, customers, packages, etc.)
  - Removes all WorkflowEngine database records (process definitions, workflows, requests, etc.)
  - Temporarily disables foreign key constraints to prevent deletion order issues
  - Handles soft-delete filters by using `IgnoreQueryFilters()`
  - Safe cleanup with try-finally blocks that re-enable constraints

### 2. Updated `Program.cs`
- **File**: `src\DDFC.API\Program.cs`
- **Change**: Added `DatabaseCleanup.ClearAllDataAsync()` call before seeding
- **Order**:
  1. Migrate databases
  2. **Clear all data** (NEW)
  3. Seed fresh data (if any)

### 3. Emptied Seeder Files
- **`DDFCDataSeeder.cs`**: Now returns without seeding any data
- **`DDFCWorkflowSeeder.cs`**: Now returns `Guid.Empty` without seeding
- **`RevisedPlanWorkflowSeeder.cs`**: Now returns `Guid.Empty` without seeding
- **`AsBuiltPlanWorkflowSeeder.cs`**: Now returns `Guid.Empty` without seeding

## What Gets Deleted

### DDFC Database
- **Identity**: All users and roles
- **Lookups**: All departments
- **Master Data**: All customers, plots, packages
- **Transactional Data**: All appointments, requests, payments, tasks, etc.
- **Document Data**: All architectural plans, reports, surveys, etc.
- **Support Data**: All tickets, replies, notifications

### WorkflowEngine Database
- **Processes**: All process definitions
- **Workflows**: All workflow requests, steps, and transitions
- **Actions**: All workflow actions and history

## How It Works

On each application startup:

1. Databases are migrated to latest schema
2. All foreign key constraints are disabled
3. All data is deleted from all tables (respecting soft-delete filters)
4. All foreign key constraints are re-enabled
5. Application starts with clean databases
6. Seeder files are called but do nothing (you can populate them with fresh seed data)

## To Re-Enable Seeding

If you want to populate the databases with fresh seed data:

1. Add back seeding logic to `DDFCDataSeeder.SeedAsync()`
2. Add back seeding logic to `DDFCWorkflowSeeder.SeedAsync()`
3. Add back seeding logic to `RevisedPlanWorkflowSeeder.SeedAsync()`
4. Add back seeding logic to `AsBuiltPlanWorkflowSeeder.SeedAsync()`

## SQL Used

The cleanup uses SQL Server batch operations:
- `EXEC sp_MSForEachTable` to iterate all tables
- `NOCHECK CONSTRAINT ALL` to disable foreign keys
- `ExecuteDeleteAsync()` for bulk deletion (much faster than individual deletes)
- `CHECK CONSTRAINT ALL` to re-enable foreign keys

This ensures fast and clean deletion without constraint violations.
