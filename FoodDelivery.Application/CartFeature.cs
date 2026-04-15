namespace FoodDelivery.Application.Features.Cart;

using AutoMapper;
using FoodDelivery.Application.Common.Abstractions;
using FoodDelivery.Application.Common.Validation;
using FoodDelivery.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

public class CartItemDto
{
    public Guid CartItemId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
}

public class CartDto
{
    public Guid CartId { get; set; }
    public IReadOnlyCollection<CartItemDto> Items { get; set; } = [];
    public decimal TotalAmount => Items.Sum(x => x.LineTotal);
}

public class AddCartItemCommand
{
    public Guid ProductId { get; init; }
    public int Quantity { get; init; } = 1;
}

public class UpdateCartItemCommand
{
    public Guid CartItemId { get; init; }
    public int Quantity { get; init; }
}

public class AddCartItemCommandValidator : AbstractValidator<AddCartItemCommand>
{
    public AddCartItemCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(50);
    }
}

public class UpdateCartItemCommandValidator : AbstractValidator<UpdateCartItemCommand>
{
    public UpdateCartItemCommandValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(50);
    }
}

public interface IGetCartQueryHandler { Task<CartDto> Handle(CancellationToken cancellationToken = default); }
public interface IAddCartItemCommandHandler { Task Handle(AddCartItemCommand command, CancellationToken cancellationToken = default); }
public interface IUpdateCartItemCommandHandler { Task Handle(UpdateCartItemCommand command, CancellationToken cancellationToken = default); }
public interface IDeleteCartItemCommandHandler { Task Handle(Guid cartItemId, CancellationToken cancellationToken = default); }

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

public sealed class DeleteCartItemCommandHandler(IApplicationDbContext context, IUnitOfWork unitOfWork) : IDeleteCartItemCommandHandler
{
    public async Task Handle(Guid cartItemId, CancellationToken cancellationToken = default)
    {
        var item = await context.Query<CartItem>().FirstOrDefaultAsync(x => x.Id == cartItemId, cancellationToken)
            ?? throw new NotFoundException($"Cart item with id '{cartItemId}' was not found.");

        context.Remove(item);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
