using FoodDelivery.Application.Common.Abstractions;
using FoodDelivery.Domain.Entities;
using FoodDelivery.Domain.Enums;
using FoodDelivery.Application.Common.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace FoodDelivery.Application.Features.Auth;

public sealed class RegisterUserCommandHandler(
    IApplicationDbContext context,
    IUnitOfWork unitOfWork,
    IPasswordService passwordService,
    IJwtTokenService jwtTokenService,
    IRefreshTokenService refreshTokenService) : IRegisterUserCommandHandler
{
    public async Task<AuthResponseDto> Handle(RegisterUserCommand command, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = command.Email.Trim().ToLowerInvariant();
        var phone = command.Phone.Trim();

        // Both columns have unique indexes; checking first turns a database error (500) into a clear 409.
        if (await context.Query<User>().AnyAsync(x => x.Email == normalizedEmail, cancellationToken))
        {
            throw new ConflictException("User with this email already exists.");
        }

        if (await context.Query<User>().AnyAsync(x => x.Phone == phone, cancellationToken))
        {
            throw new ConflictException("User with this phone number already exists.");
        }

        var user = new User
        {
            Email = normalizedEmail,
            FullName = command.FullName.Trim(),
            Phone = phone,
            Address = command.Address.Trim(),
            PasswordHash = passwordService.HashPassword(command.Password),
            Role = UserRole.Customer
        };

        var refreshTokenData = refreshTokenService.GenerateRefreshToken();
        await context.AddAsync(user, cancellationToken);
        await context.AddAsync(new RefreshToken
        {
            User = user,
            TokenHash = refreshTokenData.TokenHash,
            ExpiresAtUtc = refreshTokenData.ExpiresAtUtc,
            CreatedByIp = command.IpAddress
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return AuthMapper.BuildAuthResponse(user, refreshTokenData.Token, jwtTokenService);
    }
}
