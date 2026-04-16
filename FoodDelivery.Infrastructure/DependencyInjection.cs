using FoodDelivery.Application.Common.Abstractions;
using FoodDelivery.Infrastructure.Persistence;
using FoodDelivery.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDelivery.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=sqlserver,1433;Database=FoodDeliveryDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True";

        services.AddDbContext<FoodDeliveryDbContext>(options => options.UseSqlServer(connectionString));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
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
