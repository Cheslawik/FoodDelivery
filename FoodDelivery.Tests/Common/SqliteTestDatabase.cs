using FoodDelivery.Domain.Entities;
using FoodDelivery.Domain.Enums;
using FoodDelivery.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FoodDelivery.Tests.Common;

/// <summary>
/// In-memory SQLite database for handler tests. Unlike the EF Core InMemory provider,
/// SQLite enforces unique indexes and foreign keys, so tests see the same constraints as production.
/// </summary>
public sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteTestDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    /// <summary>Creates a fresh context over the same database, so each "request" gets its own change tracker.</summary>
    public FoodDeliveryDbContext CreateContext()
        => new(new DbContextOptionsBuilder<FoodDeliveryDbContext>().UseSqlite(_connection).Options);

    public async Task<User> AddUserAsync(string email, string phone)
    {
        await using var context = CreateContext();
        var user = new User
        {
            Email = email,
            FullName = "Test User",
            Phone = phone,
            Address = "1 Test Street",
            PasswordHash = "hash",
            Role = UserRole.Customer
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    public async Task<Product> AddProductAsync(string name, decimal price, bool isAvailable = true)
    {
        await using var context = CreateContext();
        var category = await context.Categories.FirstOrDefaultAsync() ?? new Category { Name = "Test" };
        var product = new Product
        {
            Name = name,
            Description = $"{name} description",
            Price = price,
            IsAvailable = isAvailable,
            Category = category
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();
        return product;
    }

    public void Dispose() => _connection.Dispose();
}
