using FoodDelivery.Application.Common.Abstractions;
using FoodDelivery.Infrastructure.Persistence;
using FoodDelivery.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace FoodDelivery.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<FoodDeliveryDbContext>(options => options.UseSqlServer(connectionString));
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer) && !string.IsNullOrWhiteSpace(o.Audience), "Jwt:Issuer and Jwt:Audience are required.")
            .Validate(o => o.Key is not null && Encoding.UTF8.GetByteCount(o.Key) >= JwtOptions.MinKeyBytes, $"Jwt:Key must be at least {JwtOptions.MinKeyBytes} bytes (HMAC-SHA256).")
            .ValidateOnStart();
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<FoodDeliveryDbContext>());
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<FoodDeliveryDbContext>());
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IPasswordService, PasswordService>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IRefreshTokenService, RefreshTokenService>();

        return services;
    }
}
