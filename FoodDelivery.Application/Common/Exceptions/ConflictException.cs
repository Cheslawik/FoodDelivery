namespace FoodDelivery.Application.Common.Exceptions;

/// <summary>The request conflicts with existing data, e.g. a duplicate email. Maps to 409.</summary>
public sealed class ConflictException(string message) : Exception(message);
