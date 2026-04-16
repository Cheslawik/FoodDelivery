using FoodDelivery.Application.Common.Abstractions;
using FoodDelivery.Application.Common.Validation;
using FoodDelivery.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodDelivery.Application.Features.Cart;

public sealed class AddCartItemCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IUnitOfWork unitOfWork) : IAddCartItemCommandHandler
{
    public async Task Handle(AddCartItemCommand command, CancellationToken cancellationToken = default)
    {
        var product = await context.Query<Product>().FirstOrDefaultAsync(x => x.Id == command.ProductId && x.IsAvailable, cancellationToken)
            ?? throw new NotFoundException($"Product with id '{command.ProductId}' was not found.");

        var userId = currentUserService.GetCurrentUserId();
        var cart = await context.Query<Domain.Entities.Cart>().Include(x => x.Items).FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (cart is null)
        {
            cart = new Domain.Entities.Cart { UserId = userId };
            await context.AddAsync(cart, cancellationToken);
        }

        var item = cart.Items.FirstOrDefault(x => x.ProductId == product.Id);
        if (item is null)
        {
            var newItem = new CartItem
            {
                CartId = cart.Id,
                ProductId = product.Id,
                Quantity = command.Quantity
            };

            await context.AddAsync(newItem, cancellationToken);
        }
        else
        {
            item.Quantity = Math.Min(50, item.Quantity + command.Quantity);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
