using AutoMapper;
using AutoMapper.QueryableExtensions;
using FoodDelivery.Application.Common.Abstractions;
using FoodDelivery.Application.Common.Validation;
using FoodDelivery.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodDelivery.Application.Features.Orders;

public sealed class GetOrderByIdQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IMapper mapper) : IGetOrderByIdQueryHandler
{
    public async Task<OrderDto> Handle(Guid orderId, CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.GetCurrentUserId();
        var order = await context.Query<Order>()
            .AsNoTracking()
            .Where(x => x.Id == orderId && x.UserId == userId)
            .ProjectTo<OrderDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

        return order ?? throw new NotFoundException($"Order with id '{orderId}' was not found.");
    }
}
