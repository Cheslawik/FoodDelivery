namespace FoodDelivery.Application.Features.Auth
{
    using FoodDelivery.Application.Common.Abstractions;
    using FoodDelivery.Domain.Entities;
    using FoodDelivery.Domain.Enums;
    using FluentValidation;
    using Microsoft.EntityFrameworkCore;

    public class RegisterUserCommand
    {
        public string Email { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string Phone { get; init; } = string.Empty;
        public string Address { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public string IpAddress { get; init; } = "unknown";
    }

    public class LoginCommand
    {
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public string IpAddress { get; init; } = "unknown";
    }

    public class RefreshTokenCommand
    {
        public string RefreshToken { get; init; } = string.Empty;
        public string IpAddress { get; init; } = "unknown";
    }

    public class RevokeRefreshTokenCommand
    {
        public string RefreshToken { get; init; } = string.Empty;
    }

    public class AuthResponseDto
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class RevokeRefreshTokenResponseDto
    {
        public bool Revoked { get; set; }
    }

    public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
    {
        public RegisterUserCommandValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.FullName).NotEmpty().MaximumLength(128);
            RuleFor(x => x.Phone).NotEmpty().MaximumLength(32);
            RuleFor(x => x.Address).NotEmpty().MaximumLength(256);
            RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        }
    }

    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty();
        }
    }

    public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
    {
        public RefreshTokenCommandValidator()
        {
            RuleFor(x => x.RefreshToken).NotEmpty();
        }
    }

    public class RevokeRefreshTokenCommandValidator : AbstractValidator<RevokeRefreshTokenCommand>
    {
        public RevokeRefreshTokenCommandValidator()
        {
            RuleFor(x => x.RefreshToken).NotEmpty();
        }
    }

    public interface IRegisterUserCommandHandler
    {
        Task<AuthResponseDto> Handle(RegisterUserCommand command, CancellationToken cancellationToken = default);
    }

    public interface ILoginCommandHandler
    {
        Task<AuthResponseDto> Handle(LoginCommand command, CancellationToken cancellationToken = default);
    }

    public interface IRefreshTokenCommandHandler
    {
        Task<AuthResponseDto> Handle(RefreshTokenCommand command, CancellationToken cancellationToken = default);
    }

    public interface IRevokeRefreshTokenCommandHandler
    {
        Task<RevokeRefreshTokenResponseDto> Handle(RevokeRefreshTokenCommand command, CancellationToken cancellationToken = default);
    }

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
            var exists = await context.Query<User>().AnyAsync(x => x.Email == normalizedEmail, cancellationToken);
            if (exists)
            {
                throw new ValidationException("User with this email already exists.");
            }

            var user = new User
            {
                Email = normalizedEmail,
                FullName = command.FullName.Trim(),
                Phone = command.Phone.Trim(),
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

    public sealed class LoginCommandHandler(
        IApplicationDbContext context,
        IPasswordService passwordService,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        IUnitOfWork unitOfWork) : ILoginCommandHandler
    {
        public async Task<AuthResponseDto> Handle(LoginCommand command, CancellationToken cancellationToken = default)
        {
            var normalizedEmail = command.Email.Trim().ToLowerInvariant();
            var user = await context.Query<User>().FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);
            if (user is null || !passwordService.VerifyPassword(user.PasswordHash, command.Password))
            {
                throw new ValidationException("Invalid email or password.");
            }

            var refreshTokenData = refreshTokenService.GenerateRefreshToken();
            await context.AddAsync(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = refreshTokenData.TokenHash,
                ExpiresAtUtc = refreshTokenData.ExpiresAtUtc,
                CreatedByIp = command.IpAddress
            }, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return AuthMapper.BuildAuthResponse(user, refreshTokenData.Token, jwtTokenService);
        }
    }

    public sealed class RefreshTokenCommandHandler(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService) : IRefreshTokenCommandHandler
    {
        public async Task<AuthResponseDto> Handle(RefreshTokenCommand command, CancellationToken cancellationToken = default)
        {
            var tokenHash = refreshTokenService.HashToken(command.RefreshToken);
            var existingToken = await context.Query<RefreshToken>()
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

            if (existingToken is null || !existingToken.IsActive || existingToken.User is null)
            {
                throw new ValidationException("Refresh token is invalid or expired.");
            }

            var rotatedToken = refreshTokenService.GenerateRefreshToken();
            existingToken.RevokedAtUtc = DateTime.UtcNow;
            existingToken.ReplacedByTokenHash = rotatedToken.TokenHash;
            context.Update(existingToken);

            await context.AddAsync(new RefreshToken
            {
                UserId = existingToken.UserId,
                TokenHash = rotatedToken.TokenHash,
                ExpiresAtUtc = rotatedToken.ExpiresAtUtc,
                CreatedByIp = command.IpAddress
            }, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return AuthMapper.BuildAuthResponse(existingToken.User, rotatedToken.Token, jwtTokenService);
        }
    }

    public sealed class RevokeRefreshTokenCommandHandler(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        IRefreshTokenService refreshTokenService) : IRevokeRefreshTokenCommandHandler
    {
        public async Task<RevokeRefreshTokenResponseDto> Handle(RevokeRefreshTokenCommand command, CancellationToken cancellationToken = default)
        {
            var tokenHash = refreshTokenService.HashToken(command.RefreshToken);
            var existingToken = await context.Query<RefreshToken>().FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

            if (existingToken is null || existingToken.IsRevoked)
            {
                return new RevokeRefreshTokenResponseDto { Revoked = false };
            }

            existingToken.RevokedAtUtc = DateTime.UtcNow;
            context.Update(existingToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new RevokeRefreshTokenResponseDto { Revoked = true };
        }
    }

    internal static class AuthMapper
    {
        public static AuthResponseDto BuildAuthResponse(User user, string refreshToken, IJwtTokenService jwtTokenService)
            => new()
            {
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Phone = user.Phone,
                Address = user.Address,
                Role = user.Role.ToString(),
                AccessToken = jwtTokenService.GenerateToken(user.Id, user.Email, user.FullName, user.Role.ToString()),
                RefreshToken = refreshToken
            };
    }
}
