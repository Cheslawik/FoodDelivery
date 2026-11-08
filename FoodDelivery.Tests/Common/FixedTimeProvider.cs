namespace FoodDelivery.Tests.Common;

public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
