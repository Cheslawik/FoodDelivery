using System.Net;

namespace FoodDelivery.Tests.Api;

public class RateLimitingTests
{
    [Fact]
    public async Task MoreThan100RequestsPerMinute_Returns429()
    {
        // Own factory: the limiter state must not leak into other API tests.
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        for (var i = 0; i < 100; i++)
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/menu?pageSize=1")).StatusCode);
        }

        var limited = await client.GetAsync("/api/v1/menu?pageSize=1");

        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
    }
}
