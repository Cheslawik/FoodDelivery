using FoodDelivery.Application.Features.Orders;
using FoodDelivery.Tests.Common;

namespace FoodDelivery.Tests.Application;

public class CreateOrderCommandValidatorTests
{
    private static readonly DateTime Now = new(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);
    private readonly CreateOrderCommandValidator _validator = new(new FixedTimeProvider(Now));

    [Fact]
    public void Should_Fail_When_NotAsap_And_ScheduledIsNull()
    {
        var result = _validator.Validate(ValidCommand() with { IsAsap = false, ScheduledDeliveryTimeUtc = null });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(CreateOrderCommand.ScheduledDeliveryTimeUtc));
    }

    [Fact]
    public void Should_Fail_When_ScheduledTimeIsInThePast()
    {
        var result = _validator.Validate(ValidCommand() with { IsAsap = false, ScheduledDeliveryTimeUtc = Now.AddMinutes(-1) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(CreateOrderCommand.ScheduledDeliveryTimeUtc));
    }

    [Fact]
    public void Should_Pass_When_ScheduledTimeIsInTheFuture()
    {
        var result = _validator.Validate(ValidCommand() with { IsAsap = false, ScheduledDeliveryTimeUtc = Now.AddHours(2) });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Should_Pass_When_Asap_WithoutScheduledTime()
    {
        Assert.True(_validator.Validate(ValidCommand()).IsValid);
    }

    [Theory]
    [InlineData("", "+123", "Street 1")]
    [InlineData("Test", "", "Street 1")]
    [InlineData("Test", "+123", "")]
    public void Should_Fail_When_ContactFieldIsEmpty(string name, string phone, string address)
    {
        var result = _validator.Validate(ValidCommand() with { ContactName = name, ContactPhone = phone, DeliveryAddress = address });

        Assert.False(result.IsValid);
    }

    private static CreateOrderCommand ValidCommand() => new()
    {
        ContactName = "Test",
        ContactPhone = "+123",
        DeliveryAddress = "Street 1",
        IsAsap = true
    };
}
