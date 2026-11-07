using FoodDelivery.Application.Features.Cart;
using FoodDelivery.Domain.Entities;
using FoodDelivery.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace FoodDelivery.Tests.Application;

public sealed class CartHandlersTests : IDisposable
{
    private readonly SqliteTestDatabase _db = new();

    [Fact]
    public async Task AddItem_CreatesCart_OnFirstAdd()
    {
        var user = await _db.AddUserAsync("a@test.local", "+100");
        var product = await _db.AddProductAsync("Pizza", 10m);

        await AddAsync(user.Id, product.Id, 2);

        await using var context = _db.CreateContext();
        var cart = await context.Carts.Include(x => x.Items).SingleAsync(x => x.UserId == user.Id);
        var item = Assert.Single(cart.Items);
        Assert.Equal(product.Id, item.ProductId);
        Assert.Equal(2, item.Quantity);
    }

    [Fact]
    public async Task AddItem_SameProductTwice_MergesQuantityAndCapsAt50()
    {
        var user = await _db.AddUserAsync("a@test.local", "+100");
        var product = await _db.AddProductAsync("Pizza", 10m);

        await AddAsync(user.Id, product.Id, 30);
        await AddAsync(user.Id, product.Id, 30);

        await using var context = _db.CreateContext();
        var item = await context.CartItems.SingleAsync();
        Assert.Equal(50, item.Quantity);
    }

    [Fact]
    public async Task GetCart_ReturnsLineTotalsAndCartTotal()
    {
        var user = await _db.AddUserAsync("a@test.local", "+100");
        var pizza = await _db.AddProductAsync("Pizza", 9.99m);
        var cola = await _db.AddProductAsync("Cola", 2.40m);
        await AddAsync(user.Id, pizza.Id, 2);
        await AddAsync(user.Id, cola.Id, 3);

        await using var context = _db.CreateContext();
        var cart = await CreateGetCartHandler(context, user.Id).Handle();

        Assert.Equal(2, cart.Items.Count);
        Assert.Equal(19.98m, cart.Items.Single(x => x.ProductId == pizza.Id).LineTotal);
        Assert.Equal(19.98m + 7.20m, cart.TotalAmount);
    }

    [Fact]
    public async Task GetCart_WithoutCart_ReturnsEmptyCart()
    {
        var user = await _db.AddUserAsync("a@test.local", "+100");

        await using var context = _db.CreateContext();
        var cart = await CreateGetCartHandler(context, user.Id).Handle();

        Assert.Empty(cart.Items);
        Assert.Equal(0m, cart.TotalAmount);
    }

    private async Task AddAsync(Guid userId, Guid productId, int quantity)
    {
        await using var context = _db.CreateContext();
        var handler = new AddCartItemCommandHandler(context, new FakeCurrentUser(userId), context);
        await handler.Handle(new AddCartItemCommand { ProductId = productId, Quantity = quantity });
    }

    private static GetCartQueryHandler CreateGetCartHandler(Infrastructure.Persistence.FoodDeliveryDbContext context, Guid userId)
        => new(context, new FakeCurrentUser(userId), TestMapper.Instance);

    public void Dispose() => _db.Dispose();
}
