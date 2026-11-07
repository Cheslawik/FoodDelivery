using FoodDelivery.Application.Features.Menu;
using FoodDelivery.Tests.Common;

namespace FoodDelivery.Tests.Application;

public sealed class MenuTests : IDisposable
{
    private readonly SqliteTestDatabase _db = new();

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, GetMenuQueryValidator.MaxPageSize + 1)]
    public void Validator_RejectsInvalidPaging(int pageNumber, int pageSize)
    {
        var result = new GetMenuQueryValidator().Validate(new GetMenuQuery { PageNumber = pageNumber, PageSize = pageSize });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_AcceptsDefaults()
    {
        Assert.True(new GetMenuQueryValidator().Validate(new GetMenuQuery()).IsValid);
    }

    [Fact]
    public async Task GetMenu_HidesUnavailableProducts_AndPagesByName()
    {
        await _db.AddProductAsync("Cola", 2m);
        await _db.AddProductAsync("Burger", 10m);
        await _db.AddProductAsync("Apple Pie", 4m);
        await _db.AddProductAsync("Hidden", 1m, isAvailable: false);

        await using var context = _db.CreateContext();
        var page = await new GetMenuQueryHandler(context)
            .Handle(new GetMenuQuery { PageNumber = 2, PageSize = 2 });

        Assert.Equal(3, page.TotalCount);
        Assert.Equal(["Cola"], page.Items.Select(x => x.Name));
    }

    [Fact]
    public async Task GetMenu_FiltersBySearchText()
    {
        await _db.AddProductAsync("Pepperoni", 11m);
        await _db.AddProductAsync("Cola", 2m);

        await using var context = _db.CreateContext();
        var page = await new GetMenuQueryHandler(context)
            .Handle(new GetMenuQuery { Search = "Pepper" });

        Assert.Equal(["Pepperoni"], page.Items.Select(x => x.Name));
    }

    public void Dispose() => _db.Dispose();
}
