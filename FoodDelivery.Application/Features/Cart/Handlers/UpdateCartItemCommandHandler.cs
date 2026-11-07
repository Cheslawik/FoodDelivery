using FoodDelivery.Application.Common.Abstractions;
using FoodDelivery.Application.Common.Exceptions;
using FoodDelivery.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodDelivery.Application.Features.Cart;

public sealed class UpdateCartItemCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IUnitOfWork unitOfWork) : IUpdateCartItemCommandHandler
{
    public async Task Handle(UpdateCartItemCommand command, CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.GetCurrentUserId();

        // Scope the lookup to the caller's cart: another user's item is reported as "not found"
        // rather than "forbidden", so item ids from other carts are not disclosed.
        var item = await context.Query<CartItem>()
            .FirstOrDefaultAsync(x => x.Id == command.CartItemId && x.Cart!.UserId == userId, cancellationToken)
            ?? throw new NotFoundException($"Cart item with id '{command.CartItemId}' was not found.");

        item.Quantity = command.Quantity;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
