using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GestionEspaces.Infrastructure.Persistence;

/// <summary>
/// Creates the DbContext for EF Core design-time tools.
/// </summary>
public sealed class GestionEspacesDbContextFactory : IDesignTimeDbContextFactory<GestionEspacesDbContext>
{
    public GestionEspacesDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<GestionEspacesDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=GRONKIPC\\SQLEXPRESS;Database=GestionEspacesDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true");

        return new GestionEspacesDbContext(optionsBuilder.Options);
    }
}