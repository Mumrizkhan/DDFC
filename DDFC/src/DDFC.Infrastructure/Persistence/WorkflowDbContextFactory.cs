using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using WorkflowEngine.Infrastructure.Persistence;

namespace DDFC.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for WorkflowDbContext - used by EF Core tools at design time
/// </summary>
public class WorkflowDbContextFactory : IDesignTimeDbContextFactory<WorkflowDbContext>
{
    public WorkflowDbContext CreateDbContext(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../DDFC.API"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        // Get connection string
        var connectionString = configuration.GetConnectionString("Default");

        // Build options
        var optionsBuilder = new DbContextOptionsBuilder<WorkflowDbContext>();
        optionsBuilder.UseSqlServer(connectionString, 
            b => b.MigrationsAssembly("DDFC.Infrastructure"));

        return new WorkflowDbContext(optionsBuilder.Options);
    }
}
