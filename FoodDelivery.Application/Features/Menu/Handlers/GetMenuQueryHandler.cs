using AutoMapper;
using AutoMapper.QueryableExtensions;
using FoodDelivery.Application.Common.Abstractions;
using FoodDelivery.Application.Common.Models;
using FoodDelivery.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodDelivery.Application.Features.Menu;

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
