using FoodDelivery.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDelivery.Tests.Api;

/// <summary>
/// Hosts the real API in memory. SQL Server is swapped for in-memory SQLite; the schema is created
/// from the EF model and the regular demo seed runs on startup.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "false");
        builder.UseSetting("ConnectionStrings:DefaultConnection", "unused - replaced by SQLite below");
        builder.UseSetting("Jwt:Key", "integration-tests-signing-key-0123456789abcdef");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<FoodDeliveryDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<FoodDeliveryDbContext>>();

            _connection.Open();
            services.AddDbContext<FoodDeliveryDbContext>(options => options.UseSqlite(_connection));

            using var context = new FoodDeliveryDbContext(
                new DbContextOptionsBuilder<FoodDeliveryDbContext>().UseSqlite(_connection).Options);
            context.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
