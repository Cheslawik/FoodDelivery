using FoodDelivery.Application.Common.Abstractions;
using FoodDelivery.Application.Common.Validation;
using FoodDelivery.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodDelivery.Application.Features.Cart;

public sealed class UpdateCartItemCommandHandler(IApplicationDbContext context, IUnitOfWork unitOfWork) : IUpdateCartItemCommandHandler
{
    public async Task Handle(UpdateCartItemCommand command, CancellationToken cancellationToken = default)
    {
        var item = await context.Query<CartItem>().FirstOrDefaultAsync(x => x.Id == command.CartItemId, cancellationToken)
            ?? throw new NotFoundException($"Cart item with id '{command.CartItemId}' was not found.");

        item.Quantity = command.Quantity;
        context.Update(item);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
