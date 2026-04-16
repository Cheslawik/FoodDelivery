using AutoMapper;
using AutoMapper.QueryableExtensions;
using FoodDelivery.Application.Common.Abstractions;
using FoodDelivery.Application.Common.Validation;
using FoodDelivery.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodDelivery.Application.Features.Menu;

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
