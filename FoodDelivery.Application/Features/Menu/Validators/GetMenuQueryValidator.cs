using FluentValidation;

namespace FoodDelivery.Application.Features.Menu;

public class GetMenuQueryValidator : AbstractValidator<GetMenuQuery>
{
    public const int MaxPageSize = 100;

    public GetMenuQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, MaxPageSize);
        RuleFor(x => x.Category).MaximumLength(128);
        RuleFor(x => x.Search).MaximumLength(128);
    }
}
