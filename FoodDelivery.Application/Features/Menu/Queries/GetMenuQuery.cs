namespace FoodDelivery.Application.Features.Menu;

public class GetMenuQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? Category { get; init; }
    public string? Search { get; init; }
}
