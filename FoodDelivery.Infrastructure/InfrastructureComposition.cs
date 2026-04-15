namespace FoodDelivery.Infrastructure.Persistence
{
    using FoodDelivery.Application.Common.Abstractions;
    using FoodDelivery.Domain.Entities;
    using FoodDelivery.Domain.Enums;
    using Microsoft.EntityFrameworkCore;

    public class FoodDeliveryDbContext(DbContextOptions<FoodDeliveryDbContext> options)
        : DbContext(options), IApplicationDbContext, IUnitOfWork
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Cart> Carts => Set<Cart>();
        public DbSet<CartItem> CartItems => Set<CartItem>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        public IQueryable<T> Query<T>() where T : class => Set<T>();
        public new async Task AddAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class => await Set<T>().AddAsync(entity, cancellationToken);
        public new void Update<T>(T entity) where T : class => Set<T>().Update(entity);
        public new void Remove<T>(T entity) where T : class => Set<T>().Remove(entity);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(b =>
            {
                b.Property(x => x.Email).HasMaxLength(256).IsRequired();
                b.Property(x => x.FullName).HasMaxLength(128).IsRequired();
                b.Property(x => x.Phone).HasMaxLength(32).IsRequired();
                b.Property(x => x.Address).HasMaxLength(256).IsRequired();
                b.Property(x => x.PasswordHash).HasMaxLength(1024).IsRequired();
                b.Property(x => x.Role)
                    .HasConversion<string>()
                    .HasMaxLength(32)
                    .IsRequired();
                b.HasIndex(x => x.Email).IsUnique();
                b.HasIndex(x => x.Phone).IsUnique();
                b.HasMany(x => x.RefreshTokens)
                    .WithOne(x => x.User)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Category>(b =>
            {
                b.Property(x => x.Name).HasMaxLength(128).IsRequired();
                b.HasIndex(x => x.Name).IsUnique();
            });

            modelBuilder.Entity<Product>(b =>
            {
                b.Property(x => x.Name).HasMaxLength(128).IsRequired();
                b.Property(x => x.Description).HasMaxLength(1024);
                b.Property(x => x.Price).HasPrecision(18, 2);
                b.Property(x => x.ImageUrl).HasMaxLength(512);
                b.HasOne(x => x.Category).WithMany(x => x.Products).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Cart>(b =>
            {
                b.HasIndex(x => x.UserId).IsUnique();
                b.HasMany(x => x.Items).WithOne(x => x.Cart).HasForeignKey(x => x.CartId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Order>(b =>
            {
                b.Property(x => x.ContactName).HasMaxLength(128).IsRequired();
                b.Property(x => x.ContactPhone).HasMaxLength(32).IsRequired();
                b.Property(x => x.DeliveryAddress).HasMaxLength(256).IsRequired();
                b.Property(x => x.TotalAmount).HasPrecision(18, 2);
                b.HasMany(x => x.Items).WithOne(x => x.Order).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<OrderItem>(b =>
            {
                b.Property(x => x.ProductName).HasMaxLength(256).IsRequired();
                b.Property(x => x.UnitPrice).HasPrecision(18, 2);
            });

            modelBuilder.Entity<RefreshToken>(b =>
            {
                b.Property(x => x.TokenHash).HasMaxLength(512).IsRequired();
                b.Property(x => x.ReplacedByTokenHash).HasMaxLength(512);
                b.Property(x => x.CreatedByIp).HasMaxLength(64).IsRequired();
                b.HasIndex(x => x.TokenHash).IsUnique();
                b.HasIndex(x => x.UserId);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}

namespace FoodDelivery.Infrastructure.Services
{
    using FoodDelivery.Application.Common.Abstractions;
    using FoodDelivery.Domain.Enums;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.Options;
    using Microsoft.IdentityModel.Tokens;
    using System.Globalization;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Security.Cryptography;
    using System.Text;

    public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
    {
        public Guid GetCurrentUserId()
        {
            var claimValue = httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? httpContextAccessor.HttpContext?.User?.FindFirstValue(JwtRegisteredClaimNames.Sub);

            return Guid.TryParse(claimValue, out var userId)
                ? userId
                : Guid.Parse("11111111-1111-1111-1111-111111111111");
        }
    }

    public sealed class PasswordService : IPasswordService
    {
        private readonly PasswordHasher<string> _passwordHasher = new();

        public string HashPassword(string password)
            => _passwordHasher.HashPassword(string.Empty, password);

        public bool VerifyPassword(string hashedPassword, string providedPassword)
            => _passwordHasher.VerifyHashedPassword(string.Empty, hashedPassword, providedPassword) != PasswordVerificationResult.Failed;
    }

    public sealed class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
    {
        private readonly JwtOptions _options = options.Value;

        public string GenerateToken(Guid userId, string email, string fullName, string role)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(JwtRegisteredClaimNames.Email, email),
                new(ClaimTypes.Email, email),
                new(ClaimTypes.Name, fullName),
                new(ClaimTypes.Role, role)
            };

            var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_options.AccessTokenLifetimeMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public sealed class RefreshTokenService(IOptions<JwtOptions> options) : IRefreshTokenService
    {
        private readonly JwtOptions _options = options.Value;

        public (string Token, string TokenHash, DateTime ExpiresAtUtc) GenerateRefreshToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            var token = Convert.ToBase64String(bytes);
            var tokenHash = HashToken(token);
            var expiresAtUtc = DateTime.UtcNow.AddDays(_options.RefreshTokenLifetimeDays);
            return (token, tokenHash, expiresAtUtc);
        }

        public string HashToken(string token)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(hash).ToLower(CultureInfo.InvariantCulture);
        }
    }

    public sealed class JwtOptions
    {
        public const string SectionName = "Jwt";

        public string Issuer { get; init; } = string.Empty;
        public string Audience { get; init; } = string.Empty;
        public string Key { get; init; } = string.Empty;
        public int AccessTokenLifetimeMinutes { get; init; } = 60;
        public int RefreshTokenLifetimeDays { get; init; } = 14;
    }
}

namespace FoodDelivery.Infrastructure.Persistence.Seed
{
    using FoodDelivery.Domain.Entities;
    using FoodDelivery.Domain.Enums;
    using Microsoft.EntityFrameworkCore;

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
}

namespace FoodDelivery.Infrastructure
{
    using FoodDelivery.Application.Common.Abstractions;
    using FoodDelivery.Infrastructure.Persistence;
    using FoodDelivery.Infrastructure.Services;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? "Server=sqlserver,1433;Database=FoodDeliveryDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True";

            services.AddDbContext<FoodDeliveryDbContext>(options => options.UseSqlServer(connectionString));
            services.Configure<Services.JwtOptions>(configuration.GetSection(Services.JwtOptions.SectionName));
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
}


