using GestionEspaces.Application.Interfaces;
using GestionEspaces.Application.Interfaces.Repositories;
using GestionEspaces.Infrastructure.Persistence;
using GestionEspaces.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GestionEspaces.Infrastructure.DependencyInjection;

/// <summary>
/// Registers infrastructure services.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<GestionEspacesDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("GestionEspacesDatabase");
            options.UseSqlServer(connectionString, sqlServerOptions =>
            {
                sqlServerOptions.EnableRetryOnFailure(3);
            });
        });

        services.AddScoped<IAgentRepository, AgentRepository>();
        services.AddScoped<ISiteRepository, SiteRepository>();
        services.AddScoped<IBatimentRepository, BatimentRepository>();
        services.AddScoped<IBureauRepository, BureauRepository>();
        services.AddScoped<IActifRepository, ActifRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}