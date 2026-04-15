namespace FoodDelivery.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public sealed class FoodDeliveryDbContextFactory : IDesignTimeDbContextFactory<FoodDeliveryDbContext>
{
    public FoodDeliveryDbContext CreateDbContext(string[] args)
    {
        var connectionString = "Server=localhost,1433;Database=FoodDeliveryDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True";
        var optionsBuilder = new DbContextOptionsBuilder<FoodDeliveryDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        return new FoodDeliveryDbContext(optionsBuilder.Options);
    }
}
