using FoodDelivery.Infrastructure;
using FoodDelivery.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FoodDelivery.Tests.Infrastructure;

public class JwtOptionsValidationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("too-short-key")]
    public void MissingOrShortKey_FailsValidation(string? key)
    {
        var options = BuildOptions(key);

        Assert.Throws<OptionsValidationException>(() => options.Value);
    }

    [Fact]
    public void LongEnoughKey_PassesValidation()
    {
        var options = BuildOptions(new string('k', JwtOptions.MinKeyBytes));

        Assert.Equal("issuer", options.Value.Issuer);
    }

    private static IOptions<JwtOptions> BuildOptions(string? key)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=unused",
                ["Jwt:Issuer"] = "issuer",
                ["Jwt:Audience"] = "audience",
                ["Jwt:Key"] = key
            })
            .Build();

        return new ServiceCollection()
            .AddInfrastructure(configuration)
            .BuildServiceProvider()
            .GetRequiredService<IOptions<JwtOptions>>();
    }
}
