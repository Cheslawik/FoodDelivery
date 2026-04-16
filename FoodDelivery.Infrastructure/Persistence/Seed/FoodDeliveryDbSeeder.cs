using FoodDelivery.Domain.Entities;
using FoodDelivery.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FoodDelivery.Infrastructure.Persistence.Seed;

public static class FoodDeliveryDbSeeder
{
    public static async Task SeedAsync(FoodDeliveryDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);

        if (!await dbContext.Users.AnyAsync(cancellationToken))
        {
            var passwordService = new Services.PasswordService();
            await dbContext.Users.AddRangeAsync(
            [
                new User
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Email = "customer@fooddelivery.local",
                    FullName = "Customer User",
                    Phone = "+10000000000",
                    Address = "1 Demo Street",
                    PasswordHash = passwordService.HashPassword("Password123!"),
                    Role = UserRole.Customer
                },
                new User
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Email = "admin@fooddelivery.local",
                    FullName = "Admin User",
                    Phone = "+10000000001",
                    Address = "99 Admin Street",
                    PasswordHash = passwordService.HashPassword("Password123!"),
                    Role = UserRole.Admin
                }
            ], cancellationToken);
        }

        var categoryNames = new[] { "Pizza", "Sushi", "Drinks", "Burgers", "Desserts", "Bowls" };
        var categories = await dbContext.Categories.ToListAsync(cancellationToken);
        var categoryByName = categories.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var categoryName in categoryNames)
        {
            if (categoryByName.ContainsKey(categoryName))
            {
                continue;
            }

            var category = new Category { Name = categoryName };
            await dbContext.Categories.AddAsync(category, cancellationToken);
            categoryByName[categoryName] = category;
        }

        var productsToSeed = new[]
        {
            new { Name = "Margherita", Description = "Classic mozzarella and tomato", Price = 9.99m, ImageUrl = "https://loremflickr.com/640/380/margherita,pizza?lock=1001", Category = "Pizza" },
            new { Name = "Pepperoni", Description = "Spicy pepperoni and cheese", Price = 11.49m, ImageUrl = "https://loremflickr.com/640/380/pepperoni,pizza?lock=1002", Category = "Pizza" },
            new { Name = "BBQ Chicken Pizza", Description = "Chicken, smoky sauce and onions", Price = 12.59m, ImageUrl = "https://loremflickr.com/640/380/bbq,chicken,pizza?lock=1003", Category = "Pizza" },
            new { Name = "Four Cheese Pizza", Description = "Mozzarella, cheddar, parmesan and blue cheese", Price = 12.19m, ImageUrl = "https://commons.wikimedia.org/wiki/Special:FilePath/Four%20cheese%20pizza.jpg", Category = "Pizza" },
            new { Name = "Salmon Roll", Description = "Fresh salmon with rice", Price = 8.40m, ImageUrl = "https://loremflickr.com/640/380/salmon,sushi?lock=1005", Category = "Sushi" },
            new { Name = "California Roll", Description = "Crab mix, cucumber and avocado", Price = 7.95m, ImageUrl = "https://loremflickr.com/640/380/california,roll,sushi?lock=1006", Category = "Sushi" },
            new { Name = "Spicy Tuna Roll", Description = "Tuna with spicy mayo and sesame", Price = 8.60m, ImageUrl = "https://loremflickr.com/640/380/tuna,sushi?lock=1007", Category = "Sushi" },
            new { Name = "Classic Burger", Description = "Beef patty, cheddar and pickles", Price = 10.20m, ImageUrl = "https://loremflickr.com/640/380/classic,burger?lock=1008", Category = "Burgers" },
            new { Name = "Double Smash Burger", Description = "Two beef patties with house sauce", Price = 12.80m, ImageUrl = "https://commons.wikimedia.org/wiki/Special:FilePath/Burger%20King%20Double%20Cheeseburger%20(21262495578).jpg", Category = "Burgers" },
            new { Name = "Poke Tuna Bowl", Description = "Rice bowl with tuna, edamame and avocado", Price = 11.30m, ImageUrl = "https://loremflickr.com/640/380/poke,bowl,tuna?lock=1010", Category = "Bowls" },
            new { Name = "Teriyaki Chicken Bowl", Description = "Rice, chicken teriyaki and vegetables", Price = 10.75m, ImageUrl = "https://loremflickr.com/640/380/teriyaki,chicken,bowl?lock=1011", Category = "Bowls" },
            new { Name = "Cola", Description = "500 ml", Price = 2.40m, ImageUrl = "https://commons.wikimedia.org/wiki/Special:FilePath/HK%20drink%20Coca%20Cola%20coke%20red%20can%20window%20March%202020%20SS2.jpg", Category = "Drinks" },
            new { Name = "Orange Juice", Description = "Fresh orange juice 300 ml", Price = 3.20m, ImageUrl = "https://commons.wikimedia.org/wiki/Special:FilePath/Orange%20juice%201%20edit1.jpg", Category = "Drinks" },
            new { Name = "Cheesecake Slice", Description = "Creamy vanilla cheesecake", Price = 4.80m, ImageUrl = "https://loremflickr.com/640/380/cheesecake,dessert?lock=1014", Category = "Desserts" },
            new { Name = "Chocolate Brownie", Description = "Warm brownie with chocolate chunks", Price = 4.20m, ImageUrl = "https://loremflickr.com/640/380/chocolate,brownie?lock=1015", Category = "Desserts" }
        };

        var existingProducts = await dbContext.Products.ToListAsync(cancellationToken);
        var existingByName = existingProducts.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var productSeed in productsToSeed)
        {
            if (existingByName.TryGetValue(productSeed.Name, out var existingProduct))
            {
                if (!string.Equals(existingProduct.ImageUrl, productSeed.ImageUrl, StringComparison.OrdinalIgnoreCase))
                {
                    existingProduct.ImageUrl = productSeed.ImageUrl;
                }

                continue;
            }

            await dbContext.Products.AddAsync(new Product
            {
                Name = productSeed.Name,
                Description = productSeed.Description,
                Price = productSeed.Price,
                ImageUrl = productSeed.ImageUrl,
                IsAvailable = true,
                Category = categoryByName[productSeed.Category]
            }, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
