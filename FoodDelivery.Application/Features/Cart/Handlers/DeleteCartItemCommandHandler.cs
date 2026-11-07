using FoodDelivery.Application.Common.Abstractions;
using FoodDelivery.Application.Common.Validation;
using FoodDelivery.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodDelivery.Application.Features.Cart;

public sealed class DeleteCartItemCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IUnitOfWork unitOfWork) : IDeleteCartItemCommandHandler
{
    public async Task Handle(Guid cartItemId, CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.GetCurrentUserId();

        var item = await context.Query<CartItem>()
            .FirstOrDefaultAsync(x => x.Id == cartItemId && x.Cart!.UserId == userId, cancellationToken)
            ?? throw new NotFoundException($"Cart item with id '{cartItemId}' was not found.");

        context.Remove(item);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
