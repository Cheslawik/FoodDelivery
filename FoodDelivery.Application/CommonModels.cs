namespace FoodDelivery.Application.Common.Abstractions
{
    public interface IApplicationDbContext
    {
        IQueryable<T> Query<T>() where T : class;
        Task AddAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class;
        void Update<T>(T entity) where T : class;
        void Remove<T>(T entity) where T : class;
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }

    public interface ICurrentUserService
    {
        Guid GetCurrentUserId();
    }

    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }

    public interface IPasswordService
    {
        string HashPassword(string password);
        bool VerifyPassword(string hashedPassword, string providedPassword);
    }

    public interface IJwtTokenService
    {
        string GenerateToken(Guid userId, string email, string fullName, string role);
    }

    public interface IRefreshTokenService
    {
        (string Token, string TokenHash, DateTime ExpiresAtUtc) GenerateRefreshToken();
        string HashToken(string token);
    }
}

namespace FoodDelivery.Application.Common.Models
{
    public class PaginatedResult<T>
    {
        public required IReadOnlyCollection<T> Items { get; init; }
        public required int PageNumber { get; init; }
        public required int PageSize { get; init; }
        public required int TotalCount { get; init; }
    }
}

namespace FoodDelivery.Application.Common.Validation
{
    public class NotFoundException(string message) : Exception(message);
}
