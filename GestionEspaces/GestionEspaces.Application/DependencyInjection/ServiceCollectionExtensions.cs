using FluentValidation;
using GestionEspaces.Application.DTOs.Agents;
using GestionEspaces.Application.DTOs.Assignments;
using GestionEspaces.Application.UseCases;
using GestionEspaces.Application.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace GestionEspaces.Application.DependencyInjection;

/// <summary>
/// Registers application-layer services.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateAgentRequestValidator>();

        services.AddScoped<AgentUseCases>();
        services.AddScoped<AgentSelfServiceUseCase>();
        services.AddScoped<AssignAgentToOfficeUseCase>();
        services.AddScoped<AssignAssetToAgentUseCase>();
        services.AddScoped<CloseAffectationPosteUseCase>();
        services.AddScoped<CloseAffectationActifUseCase>();
        services.AddScoped<QueryAffectationsUseCase>();
        services.AddScoped<SiteUseCases>();
        services.AddScoped<BatimentUseCases>();
        services.AddScoped<BureauUseCases>();
        services.AddScoped<ActifUseCases>();
        services.AddScoped<AuditLogUseCases>();

        return services;
    }
}