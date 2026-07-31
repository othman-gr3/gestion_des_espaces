using GestionEspaces.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace GestionEspaces.IntegrationTests.Infrastructure;

/// <summary>
/// Starts a real SQL Server container via Testcontainers, applies EF migrations,
/// and exposes a <see cref="WebApplicationFactory{TEntryPoint}"/> pre-wired to
/// that container's connection string.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public HttpClient CreateClient(Action<WebApplicationFactory<Program>>? configure = null)
    {
        var factory = new GestionEspacesWebApplicationFactory(_container.GetConnectionString());
        configure?.Invoke(factory);
        return factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Apply EF migrations against the live container.
        var options = new DbContextOptionsBuilder<GestionEspacesDbContext>()
            .UseSqlServer(_container.GetConnectionString())
            .Options;

        await using var ctx = new GestionEspacesDbContext(options);
        await ctx.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

/// <summary>
/// A <see cref="WebApplicationFactory{T}"/> that overrides the DB connection string
/// with the Testcontainers SQL Server URL.
/// </summary>
public sealed class GestionEspacesWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public GestionEspacesWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration and replace with the container's connection string.
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<GestionEspacesDbContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<GestionEspacesDbContext>(options =>
                options.UseSqlServer(_connectionString));
        });

        // Override the JWT signing key so tests can mint valid tokens.
        builder.UseSetting("Jwt:SigningKey", AuthHelper.TestSigningKey);
        builder.UseSetting("Jwt:Issuer", AuthHelper.TestIssuer);
        builder.UseSetting("Jwt:Audience", AuthHelper.TestAudience);
    }
}
