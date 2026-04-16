using FoodDelivery.Application.Features.Auth;
using FoodDelivery.Application.Features.Cart;
using FoodDelivery.Application.Features.Menu;
using FoodDelivery.Application.Features.Orders;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDelivery.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(DependencyInjection).Assembly);
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IGetMenuQueryHandler, GetMenuQueryHandler>();
        services.AddScoped<IGetProductByIdQueryHandler, GetProductByIdQueryHandler>();

        services.AddScoped<IGetCartQueryHandler, GetCartQueryHandler>();
        services.AddScoped<IAddCartItemCommandHandler, AddCartItemCommandHandler>();
        services.AddScoped<IUpdateCartItemCommandHandler, UpdateCartItemCommandHandler>();
        services.AddScoped<IDeleteCartItemCommandHandler, DeleteCartItemCommandHandler>();

        services.AddScoped<ICreateOrderCommandHandler, CreateOrderCommandHandler>();
        services.AddScoped<IGetOrdersQueryHandler, GetOrdersQueryHandler>();
        services.AddScoped<IGetOrderByIdQueryHandler, GetOrderByIdQueryHandler>();
        services.AddScoped<IRegisterUserCommandHandler, RegisterUserCommandHandler>();
        services.AddScoped<ILoginCommandHandler, LoginCommandHandler>();
        services.AddScoped<IRefreshTokenCommandHandler, RefreshTokenCommandHandler>();
        services.AddScoped<IRevokeRefreshTokenCommandHandler, RevokeRefreshTokenCommandHandler>();

        return services;
    }
}
