using FoodDelivery.Application.Common.Models;

namespace FoodDelivery.Application.Features.Menu;

public interface IGetMenuQueryHandler
{
    Task<PaginatedResult<ProductDto>> Handle(GetMenuQuery query, CancellationToken cancellationToken = default);
}
