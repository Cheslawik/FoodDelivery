namespace FoodDelivery.Application.Features.Cart;

public class UpdateCartItemCommand
{
    public Guid CartItemId { get; init; }
    public int Quantity { get; init; }
}
