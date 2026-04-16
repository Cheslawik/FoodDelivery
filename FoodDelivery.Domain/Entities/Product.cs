using FoodDelivery.Domain.Common;

namespace FoodDelivery.Domain.Entities;

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
