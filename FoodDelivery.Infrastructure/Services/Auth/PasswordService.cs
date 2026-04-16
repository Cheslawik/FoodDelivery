using FoodDelivery.Application.Common.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace FoodDelivery.Infrastructure.Services;

public sealed class PasswordService : IPasswordService
{
    private readonly PasswordHasher<string> _passwordHasher = new();

    public string HashPassword(string password)
        => _passwordHasher.HashPassword(string.Empty, password);

    public bool VerifyPassword(string hashedPassword, string providedPassword)
        => _passwordHasher.VerifyHashedPassword(string.Empty, hashedPassword, providedPassword) != PasswordVerificationResult.Failed;
}
