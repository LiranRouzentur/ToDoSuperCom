using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskApp.Application.Mappings;
using TaskApp.Application.Services;
using TaskApp.Application.Validators;
using TaskApp.Infrastructure.Data;
using TaskApp.Infrastructure.Interceptors;
using TaskApp.Api.Endpoints;
using TaskApp.Api.Middleware;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using AspNetCoreRateLimit;

var builder = WebApplication.CreateBuilder(args);

// Override URLs from environment variable if set (for Docker)
// This must be done before ConfigureWebHostDefaults processes launchSettings.json
var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
if (!string.IsNullOrEmpty(urls))
{
    builder.WebHost.UseUrls(urls.Split(';'));
}

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure JSON serialization (ISO-8601 UTC format)
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.Configure<JsonOptions>(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

// Add API versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Version"),
        new MediaTypeApiVersionReader("version"));
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Add CORS - Restricted to specific methods and headers for security
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:3000", "http://localhost:5173" })
            .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
            .WithHeaders("Content-Type", "Authorization", "If-Match", "X-Correlation-Id", "X-Version")
            .AllowCredentials();
    });
});

// Add DbContext - Fail fast if connection string is missing
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' is required but was not found in configuration.");
}

builder.Services.AddDbContext<TaskDbContext>(options =>
{
    options.UseSqlite(connectionString);
    options.AddInterceptors(new UpdateTimestampInterceptor());
});

// Add Application services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITaskService, TaskService>();

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<UserCreateRequestValidator>();

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(ApplicationMappingProfile));

// Add rate limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.EnableEndpointRateLimiting = true;
    options.StackBlockedRequests = false;
    options.HttpStatusCode = 429;
    options.RealIpHeader = "X-Real-IP";
    options.ClientIdHeader = "X-ClientId";
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "*",
            Period = "1m",
            Limit = 100
        }
    };
});
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddInMemoryRateLimiting();

var app = builder.Build();

// Initialize database on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var dbContext = services.GetRequiredService<TaskDbContext>();
    
    try
    {
        logger.LogInformation("Initializing database...");
        // Ensure Database is created and migrated (skip migrations for InMemory database)
        var providerName = dbContext.Database.ProviderName;
        if (providerName != null && providerName.Contains("InMemory"))
        {
            await dbContext.Database.EnsureCreatedAsync();
        }
        else
        {
            await dbContext.Database.MigrateAsync();
        }
        
        // Seed data for development
        if (app.Environment.IsDevelopment())
        {
            logger.LogInformation("Seeding database...");
            await TaskApp.Infrastructure.Data.DbSeeder.SeedAsync(dbContext);
        }

        logger.LogInformation("Database initialized successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing the database");
        throw;
    }
}

// Configure the HTTP request pipeline
// Swagger only in Development for security
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add rate limiting middleware
app.UseIpRateLimiting();

// Add security headers middleware
app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseCors();

// Add request size limits (1MB for JSON payloads)
app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    if (context.Request.ContentLength > 1048576) // 1MB
    {
        context.Response.StatusCode = 413; // Payload Too Large
        await context.Response.WriteAsync("Request payload too large. Maximum size is 1MB.");
        return;
    }
    await next();
});

// Add error handling middleware
app.UseMiddleware<ErrorHandlingMiddleware>();

// Map health check endpoint (early in pipeline, after middleware setup)
// This endpoint is available as soon as HTTP server starts
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithTags("Health")
    .WithName("HealthCheck");

// Map endpoints
app.MapUsersEndpoints();
app.MapTasksEndpoints();

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
