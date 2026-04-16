using AutoMapper;
using FoodDelivery.Application.Features.Cart;
using FoodDelivery.Application.Features.Menu;
using FoodDelivery.Application.Features.Orders;
using FoodDelivery.Domain.Entities;

namespace FoodDelivery.Application;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Product, ProductDto>().ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category != null ? s.Category.Name : string.Empty));

        CreateMap<Cart, CartDto>().ForMember(d => d.CartId, o => o.MapFrom(s => s.Id));
        CreateMap<CartItem, CartItemDto>()
            .ForMember(d => d.CartItemId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product != null ? s.Product.Name : string.Empty))
            .ForMember(d => d.UnitPrice, o => o.MapFrom(s => s.Product != null ? s.Product.Price : 0m));

        CreateMap<Order, OrderDto>()
            .ForMember(d => d.OrderId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.DeliveryType, o => o.MapFrom(s => s.DeliveryType.ToString()));
        CreateMap<OrderItem, OrderItemDto>();
    }
}
