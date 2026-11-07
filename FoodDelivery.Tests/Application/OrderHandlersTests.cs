using FoodDelivery.Application.Common.Exceptions;
using FoodDelivery.Application.Features.Cart;
using FoodDelivery.Application.Features.Orders;
using FoodDelivery.Domain.Enums;
using FoodDelivery.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace FoodDelivery.Tests.Application;

public sealed class OrderHandlersTests : IDisposable
{
    private readonly SqliteTestDatabase _db = new();

    [Fact]
    public async Task CreateOrder_EmptyCart_ThrowsBusinessRule()
    {
        var user = await _db.AddUserAsync("a@test.local", "+100");

        await Assert.ThrowsAsync<BusinessRuleException>(() => CreateOrderAsync(user.Id, AsapCommand()));
    }

    [Fact]
    public async Task CreateOrder_SnapshotsItems_ComputesTotal_AndEmptiesCart()
    {
        var user = await _db.AddUserAsync("a@test.local", "+100");
        var pizza = await _db.AddProductAsync("Pizza", 9.99m);
        var cola = await _db.AddProductAsync("Cola", 2.40m);
        await AddToCartAsync(user.Id, pizza.Id, 2);
        await AddToCartAsync(user.Id, cola.Id, 1);

        var orderId = await CreateOrderAsync(user.Id, AsapCommand());

        await using var context = _db.CreateContext();
        var order = await context.Orders.Include(x => x.Items).SingleAsync(x => x.Id == orderId);
        Assert.Equal(user.Id, order.UserId);
        Assert.Equal(DeliveryType.AsSoonAsPossible, order.DeliveryType);
        Assert.Null(order.ScheduledDeliveryTimeUtc);
        Assert.Equal(22.38m, order.TotalAmount);
        var pizzaLine = order.Items.Single(x => x.ProductId == pizza.Id);
        Assert.Equal(("Pizza", 9.99m, 2), (pizzaLine.ProductName, pizzaLine.UnitPrice, pizzaLine.Quantity));
        Assert.False(await context.CartItems.AnyAsync());
    }

    [Fact]
    public async Task CreateOrder_Scheduled_KeepsScheduledTime()
    {
        var user = await _db.AddUserAsync("a@test.local", "+100");
        var pizza = await _db.AddProductAsync("Pizza", 10m);
        await AddToCartAsync(user.Id, pizza.Id, 1);
        var slot = new DateTime(2030, 1, 1, 18, 0, 0, DateTimeKind.Utc);

        var orderId = await CreateOrderAsync(user.Id, AsapCommand() with { IsAsap = false, ScheduledDeliveryTimeUtc = slot });

        await using var context = _db.CreateContext();
        var order = await context.Orders.SingleAsync(x => x.Id == orderId);
        Assert.Equal(DeliveryType.Scheduled, order.DeliveryType);
        Assert.Equal(slot, order.ScheduledDeliveryTimeUtc);
    }

    [Fact]
    public async Task CreateOrder_ProductBecameUnavailable_ThrowsBusinessRule_AndKeepsCart()
    {
        var user = await _db.AddUserAsync("a@test.local", "+100");
        var pizza = await _db.AddProductAsync("Pizza", 10m);
        await AddToCartAsync(user.Id, pizza.Id, 1);
        await using (var context = _db.CreateContext())
        {
            (await context.Products.SingleAsync(x => x.Id == pizza.Id)).IsAvailable = false;
            await context.SaveChangesAsync();
        }

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => CreateOrderAsync(user.Id, AsapCommand()));

        Assert.Contains("Pizza", error.Message);
        await using var check = _db.CreateContext();
        Assert.False(await check.Orders.AnyAsync());
        Assert.True(await check.CartItems.AnyAsync());
    }

    [Fact]
    public async Task GetOrders_ReturnsOnlyCallersOrders_NewestFirst()
    {
        var user = await _db.AddUserAsync("a@test.local", "+100");
        var other = await _db.AddUserAsync("b@test.local", "+200");
        var pizza = await _db.AddProductAsync("Pizza", 10m);

        await AddToCartAsync(user.Id, pizza.Id, 1);
        var first = await CreateOrderAsync(user.Id, AsapCommand());
        await AddToCartAsync(user.Id, pizza.Id, 2);
        var second = await CreateOrderAsync(user.Id, AsapCommand());
        await AddToCartAsync(other.Id, pizza.Id, 1);
        await CreateOrderAsync(other.Id, AsapCommand());

        await using var context = _db.CreateContext();
        var orders = await new GetOrdersQueryHandler(context, new FakeCurrentUser(user.Id)).Handle();

        Assert.Equal([second, first], orders.Select(x => x.OrderId));
    }

    [Fact]
    public async Task GetOrderById_OtherUsersOrder_ThrowsNotFound()
    {
        var owner = await _db.AddUserAsync("a@test.local", "+100");
        var stranger = await _db.AddUserAsync("b@test.local", "+200");
        var pizza = await _db.AddProductAsync("Pizza", 10m);
        await AddToCartAsync(owner.Id, pizza.Id, 1);
        var orderId = await CreateOrderAsync(owner.Id, AsapCommand());

        await using var context = _db.CreateContext();
        var handler = new GetOrderByIdQueryHandler(context, new FakeCurrentUser(stranger.Id));

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(orderId));
    }

    private static CreateOrderCommand AsapCommand() => new()
    {
        ContactName = "Test",
        ContactPhone = "+100",
        DeliveryAddress = "1 Test Street",
        IsAsap = true
    };

    private async Task<Guid> CreateOrderAsync(Guid userId, CreateOrderCommand command)
    {
        await using var context = _db.CreateContext();
        return await new CreateOrderCommandHandler(context, new FakeCurrentUser(userId), context).Handle(command);
    }

    private async Task AddToCartAsync(Guid userId, Guid productId, int quantity)
    {
        await using var context = _db.CreateContext();
        await new AddCartItemCommandHandler(context, new FakeCurrentUser(userId), context)
            .Handle(new AddCartItemCommand { ProductId = productId, Quantity = quantity });
    }

    public void Dispose() => _db.Dispose();
}
