namespace FoodDelivery.Application.Common.Exceptions;

/// <summary>The requested resource does not exist or is not visible to the caller. Maps to 404.</summary>
public sealed class NotFoundException(string message) : Exception(message);
