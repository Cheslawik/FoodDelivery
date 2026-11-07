using System.Net.Http.Headers;
using System.Net.Http.Json;
using FoodDelivery.Application.Features.Auth;

namespace FoodDelivery.Tests.Api;

public static class ApiClient
{
    public const string SeededCustomerEmail = "customer@fooddelivery.local";
    public const string SeededPassword = "Password123!";

    public static async Task<AuthResponseDto> RegisterAsync(this HttpClient client, string? email = null, string? phone = null)
    {
        var unique = Guid.NewGuid().ToString("N")[..8];
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = email ?? $"user-{unique}@test.local",
            fullName = "Test User",
            phone = phone ?? $"+48{unique}",
            address = "1 Test Street",
            password = SeededPassword
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!;
    }

    /// <summary>Registers a fresh user and returns a client that sends their access token.</summary>
    public static async Task<HttpClient> CreateAuthenticatedClientAsync(this ApiFactory factory)
    {
        var client = factory.CreateClient();
        var auth = await client.RegisterAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }
}
