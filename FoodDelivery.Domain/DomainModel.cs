namespace FoodDelivery.Domain.Common
{
    public abstract class BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
    }
}

namespace FoodDelivery.Domain.Enums
{
    public enum DeliveryType
    {
        AsSoonAsPossible = 1,
        Scheduled = 2
    }

    public enum UserRole
    {
        Customer = 1,
        Admin = 2
    }
}

namespace FoodDelivery.Domain.Entities
{
    using FoodDelivery.Domain.Common;
    using FoodDelivery.Domain.Enums;

    public class User : BaseEntity
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Customer;
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }

    public class Category : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }

    public class Product : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsAvailable { get; set; } = true;
        public Guid CategoryId { get; set; }
        public Category? Category { get; set; }
    }

    public class Cart : BaseEntity
    {
        public Guid UserId { get; set; }
        public User? User { get; set; }
        public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
    }

    public class CartItem : BaseEntity
    {
        public Guid CartId { get; set; }
        public Cart? Cart { get; set; }
        public Guid ProductId { get; set; }
        public Product? Product { get; set; }
        public int Quantity { get; set; }
    }

    public class Order : BaseEntity
    {
        public Guid UserId { get; set; }
        public User? User { get; set; }
        public string DeliveryAddress { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public DeliveryType DeliveryType { get; set; }
        public DateTime? ScheduledDeliveryTimeUtc { get; set; }
        public decimal TotalAmount { get; set; }
        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }

    public class OrderItem : BaseEntity
    {
        public Guid OrderId { get; set; }
        public Order? Order { get; set; }
        public Guid ProductId { get; set; }
        public Product? Product { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
    }

    public class RefreshToken : BaseEntity
    {
        public Guid UserId { get; set; }
        public User? User { get; set; }
        public string TokenHash { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime? RevokedAtUtc { get; set; }
        public string? ReplacedByTokenHash { get; set; }
        public string CreatedByIp { get; set; } = string.Empty;

        public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
        public bool IsRevoked => RevokedAtUtc.HasValue;
        public bool IsActive => !IsExpired && !IsRevoked;
    }
}
