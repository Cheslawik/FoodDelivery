namespace FoodDelivery.Application.Common.Models;

public class PaginatedResult<T>
{
    public required IReadOnlyCollection<T> Items { get; init; }
    public required int PageNumber { get; init; }
    public required int PageSize { get; init; }
    public required int TotalCount { get; init; }
}
