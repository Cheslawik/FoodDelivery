namespace FoodDelivery.Application.Features.Menu;

public interface IGetProductByIdQueryHandler
{
    Task<ProductDto> Handle(Guid productId, CancellationToken cancellationToken = default);
}
