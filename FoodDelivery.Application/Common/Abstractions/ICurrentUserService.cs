namespace FoodDelivery.Application.Common.Abstractions;

public interface ICurrentUserService
{
    Guid GetCurrentUserId();
}
