using GestionEspaces.Application.DependencyInjection;
using GestionEspaces.Infrastructure.DependencyInjection;
using GestionEspaces.Infrastructure.Persistence;
using GestionEspaces.Api.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── JWT Authentication ──────────────────────────────────────────────────────
var jwtSection = builder.Configuration.GetSection("Jwt");
var signingKey = jwtSection["SigningKey"]
    ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");

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
// Gestionnaire   → creates/closes Affectation_Poste and Affectation_Actif, no referentiel access
// Agent          → reads only their own data (current office, assigned assets)
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ReferentielAdmin", policy =>
        policy.RequireRole("Administrateur"));

    options.AddPolicy("GestionAffectations", policy =>
        policy.RequireRole("Administrateur", "Gestionnaire"));

    options.AddPolicy("LectureAgent", policy =>
        policy.RequireRole("Agent"));
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
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
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
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<GestionEspacesDbContext>();
    await DbInitializer.SeedAsync(context);
}

app.Run();

// Required for WebApplicationFactory<Program> in integration tests.
public partial class Program { }
