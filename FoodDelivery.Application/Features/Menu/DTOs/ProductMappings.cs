using System.Linq.Expressions;
using FoodDelivery.Domain.Entities;

namespace FoodDelivery.Application.Features.Menu;

public static class ProductMappings
{
    /// <summary>Projection translated to SQL by EF Core, so only the needed columns are read.</summary>
    public static readonly Expression<Func<Product, ProductDto>> ToDto = product => new ProductDto
    {
        Id = product.Id,
        Name = product.Name,
        Description = product.Description,
        Price = product.Price,
        ImageUrl = product.ImageUrl,
        CategoryName = product.Category != null ? product.Category.Name : string.Empty
    };
}
