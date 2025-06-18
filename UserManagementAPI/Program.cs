using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using Domain.Entities;
using Domain.Interfaces;
using Application.Interfaces;
using Application.Services;
using Microsoft.Extensions.Configuration;
using Utilities.Security;
using UserManagementAPI.Middlewares;
using Microsoft.Extensions.Caching.Memory;
using AspNetCoreRateLimit;
using FluentValidation;
using Application.Validators;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Environment.EnvironmentName = "Production"; // Set environment to Production


// override URLs
builder.WebHost.UseUrls("http://0.0.0.0:5003");



// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)
    ));

// Add services
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAuthService, AuthService>();


// Validate SMTP settings
var smtpSettings = builder.Configuration.GetSection("Smtp");
if (string.IsNullOrEmpty(smtpSettings["Host"]) || string.IsNullOrEmpty(smtpSettings["Username"]))
{
    throw new InvalidOperationException("SMTP settings are not configured properly in appsettings.json.");
}

// Validate API Key
var apiKey = builder.Configuration["ApiKey"];
if (string.IsNullOrEmpty(apiKey))
{
    throw new InvalidOperationException("API Key is not configured in appsettings.json.");
}

// Configure Rate Limiting
builder.Services.AddOptions();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>();
// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<ResetPasswordValidator>();

// Configure JWT
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? "X7kP9mQ2vL5jR8yT3wZ6nB4xC1uF8hJ9kLmP3qW4rT6yU8iO9pX2vC5mN7bV1j";
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "http://localhost:5003";
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "http://localhost:5003";

if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
    throw new Exception("Invalid JWT SecretKey! It must be at least 32 characters long.");
if (string.IsNullOrWhiteSpace(jwtIssuer))
    throw new Exception("JWT Issuer is not set!");
if (string.IsNullOrWhiteSpace(jwtAudience))
    throw new Exception("JWT Audience is not set!");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// Add controllers and Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "User Management API",
        Version = "v1"
    });

    // Define the BearerAuth security scheme
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });

    // Make sure swagger uses the Bearer token
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
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
            new string[]{}
        }
    });
});

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        DbInitializer.Initialize(context);
        Console.WriteLine("Database initialization completed.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred while initializing the database: {ex.Message}");
    }
}

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGet("/", () => Results.Redirect("/swagger"));
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseSerilogRequestLogging();
    app.UseExceptionHandler("/Error");
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("HTTPS redirection and HSTS enforced in Production environment.");
}

app.UseIpRateLimiting();
app.UseMiddleware<UserManagementAPI.Middlewares.ErrorHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<UserManagementAPI.Middlewares.JwtTokenValidationMiddleware>();
app.MapControllers();

Console.WriteLine("API is running on:");
if (app.Environment.IsDevelopment())
{
    Console.WriteLine("HTTP: http://localhost:5003");
    Console.WriteLine("HTTPS: https://localhost:5004"); // Development
}
else
{
    Console.WriteLine("HTTP: http://localhost:5003");
    Console.WriteLine("HTTPS: https://localhost:5003"); // Production
}
Console.WriteLine("Swagger UI: http://localhost:5003/swagger");

app.Run();