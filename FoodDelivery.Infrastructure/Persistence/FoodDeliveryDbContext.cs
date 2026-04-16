using FoodDelivery.Application.Common.Abstractions;
using FoodDelivery.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodDelivery.Infrastructure.Persistence;

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
