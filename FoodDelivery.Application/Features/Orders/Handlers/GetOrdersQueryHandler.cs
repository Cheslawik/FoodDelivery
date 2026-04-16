using AutoMapper;
using AutoMapper.QueryableExtensions;
using FoodDelivery.Application.Common.Abstractions;
using FoodDelivery.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodDelivery.Application.Features.Orders;

public sealed class GetOrdersQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IMapper mapper) : IGetOrdersQueryHandler
{
    public async Task<IReadOnlyCollection<OrderDto>> Handle(CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.GetCurrentUserId();
        return await context.Query<Order>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ProjectTo<OrderDto>(mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
    }
}
