using Microsoft.EntityFrameworkCore;

namespace API_Althea_systems.Data;

public class AltheaDbContext : DbContext
{
    public AltheaDbContext(DbContextOptions<AltheaDbContext> options) : base(options)
    {
    }

    // DbSets will be added in Phase 3 (Models)

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Entity configurations will be applied in Phase 4 (Data)
        // modelBuilder.ApplyConfigurationsFromAssembly(typeof(AltheaDbContext).Assembly);
    }
}
