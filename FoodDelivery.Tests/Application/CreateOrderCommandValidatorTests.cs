using FoodDelivery.Application.Features.Orders;

namespace FoodDelivery.Tests;

public class CreateOrderCommandValidatorTests
{
    [Fact]
    public void Should_Fail_When_NotAsap_And_ScheduledIsNull()
    {
        var validator = new CreateOrderCommandValidator();
        var command = new CreateOrderCommand
        {
            ContactName = "Test",
            ContactPhone = "+123",
            DeliveryAddress = "Street 1",
            IsAsap = false,
            ScheduledDeliveryTimeUtc = null
        };

        var result = validator.Validate(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(CreateOrderCommand.ScheduledDeliveryTimeUtc));
    }
}
