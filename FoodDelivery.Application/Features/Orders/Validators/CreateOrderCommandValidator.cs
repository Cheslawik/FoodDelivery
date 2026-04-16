using FluentValidation;

namespace FoodDelivery.Application.Features.Orders;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.ContactName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.ContactPhone).NotEmpty().MaximumLength(32);
        RuleFor(x => x.DeliveryAddress).NotEmpty().MaximumLength(256);
        RuleFor(x => x.ScheduledDeliveryTimeUtc).NotNull().When(x => !x.IsAsap);
    }
}
