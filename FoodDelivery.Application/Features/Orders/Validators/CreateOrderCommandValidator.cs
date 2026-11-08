using FluentValidation;

namespace FoodDelivery.Application.Features.Orders;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.ContactName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.ContactPhone).NotEmpty().MaximumLength(32);
        RuleFor(x => x.DeliveryAddress).NotEmpty().MaximumLength(256);

        When(x => !x.IsAsap, () =>
        {
            RuleFor(x => x.ScheduledDeliveryTimeUtc)
                .NotNull()
                .Must(time => time > timeProvider.GetUtcNow().UtcDateTime)
                .WithMessage("Scheduled delivery time must be in the future.");
        });
    }
}
