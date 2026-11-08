using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDelivery.Application.Common.Models;
using FoodDelivery.Application.Features.Auth;
using FoodDelivery.Application.Features.Cart;
using FoodDelivery.Application.Features.Menu;
using FoodDelivery.Application.Features.Orders;

namespace FoodDelivery.Tests.Api;

public sealed class ApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string ProblemJson = "application/problem+json";

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var response = await factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Menu_IsPublic_AndReturnsSeededProductsPaged()
    {
        var page = await factory.CreateClient().GetFromJsonAsync<PaginatedResult<ProductDto>>("/api/v1/menu?pageSize=5");

        Assert.NotNull(page);
        Assert.Equal(5, page.Items.Count);
        Assert.True(page.TotalCount > 5);
        Assert.All(page.Items, x => Assert.False(string.IsNullOrEmpty(x.CategoryName)));
    }

    [Fact]
    public async Task Menu_FilterByCategory_ReturnsOnlyThatCategory()
    {
        var page = await factory.CreateClient().GetFromJsonAsync<PaginatedResult<ProductDto>>("/api/v1/menu?category=Pizza&pageSize=50");

        Assert.NotNull(page);
        Assert.NotEmpty(page.Items);
        Assert.All(page.Items, x => Assert.Equal("Pizza", x.CategoryName));
    }

    [Fact]
    public async Task Menu_InvalidPage_Returns400ValidationProblem()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/menu?pageNumber=0");

        var problem = await ReadProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.True(problem.GetProperty("errors").TryGetProperty("PageNumber", out _));
    }

    [Fact]
    public async Task Product_UnknownId_Returns404Problem()
    {
        var response = await factory.CreateClient().GetAsync($"/api/v1/menu/{Guid.NewGuid()}");

        await ReadProblemAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409Problem()
    {
        var client = factory.CreateClient();
        var first = await client.RegisterAsync();

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = first.Email,
            fullName = "Someone Else",
            phone = "+48999999999",
            address = "2 Test Street",
            password = ApiClient.SeededPassword
        });

        await ReadProblemAsync(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_InvalidPayload_Returns400WithFieldErrors()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "not-an-email",
            fullName = "",
            phone = "+48111",
            address = "1 Test Street",
            password = "short"
        });

        var errors = (await ReadProblemAsync(response, HttpStatusCode.BadRequest)).GetProperty("errors");
        Assert.True(errors.TryGetProperty("Email", out _));
        Assert.True(errors.TryGetProperty("Password", out _));
    }

    [Fact]
    public async Task Login_SeededCustomer_ReturnsTokens_AndMeWorks()
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = ApiClient.SeededCustomerEmail, password = ApiClient.SeededPassword });
        login.EnsureSuccessStatusCode();
        var auth = (await login.Content.ReadFromJsonAsync<AuthResponseDto>())!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var me = await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");

        Assert.Equal(ApiClient.SeededCustomerEmail, me.GetProperty("email").GetString());
        Assert.Equal("Customer", me.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401Problem()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new { email = ApiClient.SeededCustomerEmail, password = "wrong-password" });

        await ReadProblemAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ReturnsNewPair_AndOldTokenIsRejected()
    {
        var client = factory.CreateClient();
        var registered = await client.RegisterAsync();

        var refreshed = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = registered.RefreshToken });
        refreshed.EnsureSuccessStatusCode();
        var reused = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = registered.RefreshToken });

        await ReadProblemAsync(reused, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Cart_WithoutToken_Returns401()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/cart");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Checkout_FullFlow_CreatesOrderAndEmptiesCart()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var products = (await client.GetFromJsonAsync<PaginatedResult<ProductDto>>("/api/v1/menu?pageSize=2"))!.Items.ToList();

        (await client.PostAsJsonAsync("/api/v1/cart/items", new { productId = products[0].Id, quantity = 2 })).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/v1/cart/items", new { productId = products[1].Id, quantity = 1 })).EnsureSuccessStatusCode();

        var cart = (await client.GetFromJsonAsync<CartDto>("/api/v1/cart"))!;
        var expectedTotal = products[0].Price * 2 + products[1].Price;
        Assert.Equal(expectedTotal, cart.TotalAmount);

        var created = await client.PostAsJsonAsync("/api/v1/orders", new
        {
            contactName = "Test User",
            contactPhone = "+48123456789",
            deliveryAddress = "1 Test Street",
            isAsap = true
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotNull(created.Headers.Location);

        var order = (await client.GetFromJsonAsync<OrderDto>(created.Headers.Location))!;
        Assert.Equal(expectedTotal, order.TotalAmount);
        Assert.Equal(2, order.Items.Count);
        Assert.Equal("AsSoonAsPossible", order.DeliveryType);

        var history = (await client.GetFromJsonAsync<List<OrderDto>>("/api/v1/orders"))!;
        Assert.Contains(history, x => x.OrderId == order.OrderId);
        Assert.Empty((await client.GetFromJsonAsync<CartDto>("/api/v1/cart"))!.Items);
    }

    [Fact]
    public async Task Checkout_EmptyCart_Returns400Problem()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/orders", new
        {
            contactName = "Test User",
            contactPhone = "+48123456789",
            deliveryAddress = "1 Test Street",
            isAsap = true
        });

        await ReadProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CartItem_OfAnotherUser_CannotBeChangedOrDeleted()
    {
        var owner = await factory.CreateAuthenticatedClientAsync();
        var stranger = await factory.CreateAuthenticatedClientAsync();
        var product = (await owner.GetFromJsonAsync<PaginatedResult<ProductDto>>("/api/v1/menu?pageSize=1"))!.Items.Single();
        (await owner.PostAsJsonAsync("/api/v1/cart/items", new { productId = product.Id, quantity = 1 })).EnsureSuccessStatusCode();
        var itemId = (await owner.GetFromJsonAsync<CartDto>("/api/v1/cart"))!.Items.Single().CartItemId;

        var update = await stranger.PutAsJsonAsync($"/api/v1/cart/items/{itemId}", new { quantity = 50 });
        var delete = await stranger.DeleteAsync($"/api/v1/cart/items/{itemId}");

        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        var item = (await owner.GetFromJsonAsync<CartDto>("/api/v1/cart"))!.Items.Single();
        Assert.Equal(1, item.Quantity);
    }

    [Fact]
    public async Task Order_OfAnotherUser_Returns404()
    {
        var owner = await factory.CreateAuthenticatedClientAsync();
        var stranger = await factory.CreateAuthenticatedClientAsync();
        var product = (await owner.GetFromJsonAsync<PaginatedResult<ProductDto>>("/api/v1/menu?pageSize=1"))!.Items.Single();
        (await owner.PostAsJsonAsync("/api/v1/cart/items", new { productId = product.Id, quantity = 1 })).EnsureSuccessStatusCode();
        var created = await owner.PostAsJsonAsync("/api/v1/orders", new
        {
            contactName = "Owner",
            contactPhone = "+48123456789",
            deliveryAddress = "1 Test Street",
            isAsap = true
        });

        var response = await stranger.GetAsync(created.Headers.Location);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<JsonElement> ReadProblemAsync(HttpResponseMessage response, HttpStatusCode expectedStatus)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((int)expectedStatus, problem.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrEmpty(problem.GetProperty("title").GetString()));
        return problem;
    }
}
