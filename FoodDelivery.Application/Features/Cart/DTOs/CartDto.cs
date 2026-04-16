namespace FoodDelivery.Application.Features.Cart;

public class CartDto
{
    public Guid CartId { get; set; }
    public IReadOnlyCollection<CartItemDto> Items { get; set; } = [];
    public decimal TotalAmount => Items.Sum(x => x.LineTotal);
}
