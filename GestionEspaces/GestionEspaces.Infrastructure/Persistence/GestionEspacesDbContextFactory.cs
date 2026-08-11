using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace GestionEspaces.Infrastructure.Persistence;

/// <summary>
/// Creates the DbContext for EF Core design-time tools.
/// </summary>
public sealed class GestionEspacesDbContextFactory : IDesignTimeDbContextFactory<GestionEspacesDbContext>
{
    public GestionEspacesDbContext CreateDbContext(string[] args)
    {
        var appSettingsPath = FindAppSettingsPath();

        var configuration = new ConfigurationBuilder()
            .AddJsonFile(appSettingsPath, optional: false)
            .AddJsonFile(Path.Combine(Path.GetDirectoryName(appSettingsPath)!, "appsettings.Development.json"), optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<GestionEspacesDbContext>();
        optionsBuilder.UseSqlServer(
            configuration.GetConnectionString("GestionEspacesDatabase"));

        return new GestionEspacesDbContext(optionsBuilder.Options);
    }

    private static string FindAppSettingsPath()
    {
        var searchRoots = new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory };

        foreach (var root in searchRoots)
        {
            var currentDirectory = new DirectoryInfo(root);

            while (currentDirectory is not null)
            {
                var candidatePath = Path.Combine(currentDirectory.FullName, "GestionEspaces.Api", "appsettings.json");
                if (File.Exists(candidatePath))
                {
                    return candidatePath;
                }

                currentDirectory = currentDirectory.Parent;
            }
        }

        throw new FileNotFoundException("Could not locate GestionEspaces.Api/appsettings.json for design-time EF Core configuration.");
    }
}