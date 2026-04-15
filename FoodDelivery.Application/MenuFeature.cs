namespace FoodDelivery.Application.Features.Menu;

using AutoMapper;
using AutoMapper.QueryableExtensions;
using FoodDelivery.Application.Common.Abstractions;
using FoodDelivery.Application.Common.Models;
using FoodDelivery.Application.Common.Validation;
using FoodDelivery.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
}

public class GetMenuQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? Category { get; init; }
    public string? Search { get; init; }
}

public interface IGetMenuQueryHandler
{
    Task<PaginatedResult<ProductDto>> Handle(GetMenuQuery query, CancellationToken cancellationToken = default);
}

public interface IGetProductByIdQueryHandler
{
    Task<ProductDto> Handle(Guid productId, CancellationToken cancellationToken = default);
}

public sealed class GetMenuQueryHandler(IApplicationDbContext context, IMapper mapper) : IGetMenuQueryHandler
{
    public async Task<PaginatedResult<ProductDto>> Handle(GetMenuQuery query, CancellationToken cancellationToken = default)
    {
        IQueryable<Product> source = context.Query<Product>().AsNoTracking().Where(x => x.IsAvailable);

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            source = source.Where(x => x.Category != null && x.Category.Name == query.Category);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            source = source.Where(x => x.Name.Contains(query.Search) || x.Description.Contains(query.Search));
        }

        var totalCount = await source.CountAsync(cancellationToken);
        var items = await source
            .OrderBy(x => x.Name)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ProjectTo<ProductDto>(mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<ProductDto>
        {
            Items = items,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }
}

public sealed class GetProductByIdQueryHandler(IApplicationDbContext context, IMapper mapper) : IGetProductByIdQueryHandler
{
    public async Task<ProductDto> Handle(Guid productId, CancellationToken cancellationToken = default)
    {
        var item = await context.Query<Product>()
            .AsNoTracking()
            .Where(x => x.Id == productId && x.IsAvailable)
            .Include(x => x.Category)
            .ProjectTo<ProductDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

        return item ?? throw new NotFoundException($"Product with id '{productId}' was not found.");
    }
}

