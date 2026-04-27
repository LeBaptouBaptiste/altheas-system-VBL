using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace API_Althea_systems.Data;

/// <summary>
/// Design-time factory used by `dotnet ef` (migrations, scaffolding).
/// It bypasses the full application DI graph (which eagerly connects to Redis,
/// requires JWT secrets, etc.) and provisions only what EF tooling needs.
///
/// Reads the PostgreSQL connection string from, in priority order:
///   1. ConnectionStrings__PostgreSQL env var
///   2. appsettings.Development.json
///   3. a localhost fallback
///
/// Never used at runtime — the regular DI registration in
/// <c>ServiceCollectionExtensions.AddDatabase</c> remains authoritative.
/// </summary>
public class AltheaDbContextFactory : IDesignTimeDbContextFactory<AltheaDbContext>
{
    public AltheaDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("PostgreSQL")
            ?? "Host=localhost;Port=5432;Database=althea_systems_dev;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<AltheaDbContext>()
            .UseNpgsql(connectionString);

        return new AltheaDbContext(optionsBuilder.Options);
    }
}
