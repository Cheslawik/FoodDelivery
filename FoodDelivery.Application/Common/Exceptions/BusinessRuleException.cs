namespace FoodDelivery.Application.Common.Exceptions;

/// <summary>The request is well-formed but breaks a business rule, e.g. checking out an empty cart. Maps to 400.</summary>
public sealed class BusinessRuleException(string message) : Exception(message);
