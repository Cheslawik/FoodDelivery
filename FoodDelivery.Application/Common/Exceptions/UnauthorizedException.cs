namespace FoodDelivery.Application.Common.Exceptions;

/// <summary>The caller could not be authenticated, e.g. wrong credentials or an invalid refresh token. Maps to 401.</summary>
public sealed class UnauthorizedException(string message) : Exception(message);
