using FoodDelivery.Application.Common.Abstractions;

namespace FoodDelivery.Tests.Common;

public sealed class FakeCurrentUser(Guid userId) : ICurrentUserService
{
    public Guid GetCurrentUserId() => userId;
}
