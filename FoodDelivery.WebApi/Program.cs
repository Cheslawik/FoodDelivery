using System.Reflection;
using Asp.Versioning;
using FoodDelivery.Application;
using FoodDelivery.Infrastructure;
using FoodDelivery.Infrastructure.Persistence;
using FoodDelivery.Infrastructure.Persistence.Seed;
using FoodDelivery.Infrastructure.Services;
using FoodDelivery.WebApi.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Options;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, cfg) =>
{
    cfg.ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers(options => options.Filters.Add<ValidationFilter>());
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

// Token validation reads JwtOptions at runtime, so the key can come from user-secrets or environment variables.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((options, jwt) =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwt.Value.Issuer,
            ValidAudience = jwt.Value.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Value.Key)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Food Delivery API",
        Version = "v1",
        Description = "Front-office REST API for food delivery service"
    });

    var bearerScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT bearer token"
    };
    options.AddSecurityDefinition("Bearer", bearerScheme);
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
            []
        }
    });

    var xml = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xml);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("default", settings =>
    {
        settings.PermitLimit = 100;
        settings.Window = TimeSpan.FromMinutes(1);
        settings.QueueLimit = 10;
    });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers().RequireRateLimiting("default");
app.MapHealthChecks("/health");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FoodDeliveryDbContext>();
    if (app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", true))
    {
        await db.Database.MigrateAsync();
    }

    await FoodDeliveryDbSeeder.SeedAsync(db);
}

app.Run();

/// <summary>Entry point; public so integration tests can host the API with WebApplicationFactory.</summary>
public partial class Program;

