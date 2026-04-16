using AutoMapper;
using FoodDelivery.Application.Common.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace FoodDelivery.Application.Features.Cart;

public sealed class GetCartQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IMapper mapper) : IGetCartQueryHandler
{
    public async Task<CartDto> Handle(CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.GetCurrentUserId();
        var cart = await context.Query<Domain.Entities.Cart>()
            .AsNoTracking()
            .Include(x => x.Items).ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        return cart is null ? new CartDto { CartId = Guid.Empty, Items = [] } : mapper.Map<CartDto>(cart);
    }
}
