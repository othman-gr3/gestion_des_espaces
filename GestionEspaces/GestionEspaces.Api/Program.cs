using GestionEspaces.Application.DependencyInjection;
using GestionEspaces.Application.Interfaces;
using GestionEspaces.Infrastructure.Ai;
using GestionEspaces.Infrastructure.DependencyInjection;
using GestionEspaces.Infrastructure.Persistence;
using GestionEspaces.Api.Middleware;
using GestionEspaces.Api.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ── JWT Authentication ──────────────────────────────────────────────────────
var jwtSection = builder.Configuration.GetSection("Jwt");
var signingKey = jwtSection["SigningKey"]
    ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");

const string PlaceholderSigningKey = "__SET_VIA_ENV_GestionEspaces__Jwt__SigningKey__OR_USER_SECRETS__";
if (signingKey == PlaceholderSigningKey)
{
    // Non-blocking: the placeholder is a valid (if weak and publicly known) HMAC key, so
    // local dev keeps working without extra setup — but tokens signed with it should never
    // be trusted. Set a real key with:
    //   dotnet user-secrets set "Jwt:SigningKey" "<random-32+-char-value>"
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine(
        "AVERTISSEMENT: Jwt:SigningKey utilise la valeur placeholder committee dans appsettings.json. " +
        "Définissez votre propre clé via 'dotnet user-secrets set \"Jwt:SigningKey\" \"<valeur-aleatoire>\"' avant tout usage au-delà du développement local.");
    Console.ResetColor();
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"] ?? "GestionEspaces",
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"] ?? "GestionEspacesApi",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

// ── Authorization Policies ──────────────────────────────────────────────────
// Administrateur → full CRUD on the referentiel (Sites, Batiments, Bureaux, Agents, Actifs)
// Gestionnaire   → read-only referentiel access (to search/select for assignments) plus
//                  creates/closes Affectation_Poste and Affectation_Actif; no write rights
//                  on the referentiel itself
// Agent          → self-service portal: reads their own data (current office, assigned
//                  assets, history) and writes limited to their own profile/requests —
//                  never an arbitrary agent id from the URL
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ReferentielAdmin", policy =>
        policy.RequireRole("Administrateur"));

    options.AddPolicy("ReferentielLecture", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    options.AddPolicy("GestionAffectations", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    options.AddPolicy("AccesAgent", policy =>
        policy.RequireRole("Agent"));
});

// ── Rate Limiting ────────────────────────────────────────────────────────────
// "LoginPolicy": caps login attempts per client IP to slow down credential-guessing
// against /api/auth/login, which has no other throttling since it's anonymous by design.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("LoginPolicy", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 4,
                QueueLimit = 0
            }));

    // "AiSearchPolicy": the AI office search calls a paid external API per request —
    // authenticated users already, but still worth capping against runaway cost.
    options.AddPolicy("AiSearchPolicy", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 4,
                QueueLimit = 0
            }));
});

builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? ["http://localhost:5173"];

    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpClient<IOfficeSearchAssistant, OpenRouterOfficeSearchAssistant>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Entrez un JWT Bearer token : **Bearer {token}**"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<GestionEspacesDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    // A freshly started `docker compose up` SQL Server container may still be initializing
    // when the API boots right after it, so retry the first migration/seed attempt a few
    // times with backoff instead of crashing on the very first run.
    const int maxAttempts = 5;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await DbInitializer.SeedAsync(context);
            break;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning(ex, "Échec de la migration/seed initiale (tentative {Attempt}/{MaxAttempts}) — nouvel essai dans {DelaySeconds}s. La base de données est peut-être encore en train de démarrer.", attempt, maxAttempts, attempt * 3);
            await Task.Delay(TimeSpan.FromSeconds(attempt * 3));
        }
    }
}

app.Run();

// Required for WebApplicationFactory<Program> in integration tests.
public partial class Program { }
