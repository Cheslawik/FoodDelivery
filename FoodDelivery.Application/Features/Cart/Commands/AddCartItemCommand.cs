namespace FoodDelivery.Application.Features.Cart;

public class AddCartItemCommand
{
    public Guid ProductId { get; init; }
    public int Quantity { get; init; } = 1;
}
