namespace FoodDelivery.Application.Features.Cart;

public static class CartMappings
{
    public static CartDto ToDto(this Domain.Entities.Cart cart) => new()
    {
        CartId = cart.Id,
        Items = cart.Items
            .Select(item => new CartItemDto
            {
                CartItemId = item.Id,
                ProductId = item.ProductId,
                ProductName = item.Product?.Name ?? string.Empty,
                UnitPrice = item.Product?.Price ?? 0m,
                Quantity = item.Quantity
            })
            .ToList()
    };
}
