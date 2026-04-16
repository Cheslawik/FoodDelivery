namespace FoodDelivery.Application.Common.Validation;

public class NotFoundException(string message) : Exception(message);
