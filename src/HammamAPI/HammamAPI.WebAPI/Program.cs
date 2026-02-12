using System.Text;
using HammamAPI.Application.Services;
using HammamAPI.Application.Services.Implementations;
using HammamAPI.Domain.Interfaces;
using HammamAPI.Infrastructure.Data;
using HammamAPI.Infrastructure.Data.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configuration Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/hammam-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// En dev, utiliser http://localhost:5000 ; en prod, ASPNETCORE_URLS est injecté via Docker
if (builder.Environment.IsDevelopment())
{
    builder.WebHost.UseUrls("http://localhost:5000");
}

// Configuration base de données
var useInMemory = builder.Configuration.GetValue<bool>("UseInMemoryDatabase");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (useInMemory || string.IsNullOrEmpty(connectionString) || connectionString.Contains("YOUR_PASSWORD"))
{
    Log.Warning("⚠️ PostgreSQL non configuré - Utilisation de la base InMemory pour les tests");
    builder.Services.AddDbContext<HammamDbContext>(options =>
        options.UseInMemoryDatabase("HammamTestDb"));
}
else
{
    builder.Services.AddDbContext<HammamDbContext>(options =>
        options.UseNpgsql(connectionString));
}

// Repositories
builder.Services.AddScoped<IHammamRepository, HammamRepository>();
builder.Services.AddScoped<IEmployeRepository, EmployeRepository>();
builder.Services.AddScoped<ITypeTicketRepository, TypeTicketRepository>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<IVersementRepository, VersementRepository>();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IStatsService, StatsService>();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secret = jwtSettings["Secret"] ?? "DefaultSecretKeyForDevelopmentOnlyDoNotUseInProduction123456";
var secretKey = Encoding.UTF8.GetBytes(secret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(secretKey),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "HammamAPI",
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"] ?? "HammamClients",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// CORS - configurable par environnement
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            var origins = (builder.Configuration["CorsSettings:AllowedOrigins"]
                ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                ?? new[] { "http://localhost" };

            policy.WithOrigins(origins)
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Hammam API",
        Version = "v1",
        Description = "API pour le système de gestion des Hammams"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Entrez 'Bearer' suivi d'un espace et du token JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddResponseCompression();

var app = builder.Build();

// Seed data pour InMemory
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HammamDbContext>();
    
    if (db.Database.IsInMemory())
    {
        await db.Database.EnsureCreatedAsync();
        Log.Information("✅ Base de données InMemory initialisée avec les données seed");
    }
    else
    {
        try
        {
            await db.Database.MigrateAsync();
            Log.Information("✅ Migrations PostgreSQL appliquées");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "❌ Erreur lors de la migration. Utilisez UseInMemoryDatabase=true dans appsettings.json");
        }
    }
}

// Swagger uniquement en développement
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Hammam API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowAll");

// Servir les fichiers statiques (uploads d'images)
var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.UseAuthentication();
app.UseAuthorization();
app.UseResponseCompression();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Endpoint d'accueil
app.MapGet("/", () => Results.Redirect("/swagger"));

var urls = app.Urls.Any() ? string.Join(", ", app.Urls) : "http://localhost:5000";
Log.Information("========================================");
Log.Information("🚀 HAMMAM API DÉMARRÉE");
Log.Information("📍 Environment: {Env}", app.Environment.EnvironmentName);
Log.Information("📍 URLs: {Urls}", urls);
Log.Information("========================================");

app.Run();
