namespace FoodDelivery.Application.Features.Orders
{
    using AutoMapper;
    using AutoMapper.QueryableExtensions;
    using FoodDelivery.Application.Common.Abstractions;
    using FoodDelivery.Application.Common.Validation;
    using FoodDelivery.Domain.Entities;
    using FoodDelivery.Domain.Enums;
    using FluentValidation;
    using Microsoft.EntityFrameworkCore;

    public class OrderItemDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal LineTotal => UnitPrice * Quantity;
    }

    public class OrderDto
    {
        public Guid OrderId { get; set; }
        public string ContactName { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
        public string DeliveryType { get; set; } = string.Empty;
        public DateTime? ScheduledDeliveryTimeUtc { get; set; }
        public decimal TotalAmount { get; set; }
        public IReadOnlyCollection<OrderItemDto> Items { get; set; } = [];
    }

    public class CreateOrderCommand
    {
        public string ContactName { get; init; } = string.Empty;
        public string ContactPhone { get; init; } = string.Empty;
        public string DeliveryAddress { get; init; } = string.Empty;
        public bool IsAsap { get; init; }
        public DateTime? ScheduledDeliveryTimeUtc { get; init; }
    }

    public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
    {
        public CreateOrderCommandValidator()
        {
            RuleFor(x => x.ContactName).NotEmpty().MaximumLength(128);
            RuleFor(x => x.ContactPhone).NotEmpty().MaximumLength(32);
            RuleFor(x => x.DeliveryAddress).NotEmpty().MaximumLength(256);
            RuleFor(x => x.ScheduledDeliveryTimeUtc).NotNull().When(x => !x.IsAsap);
        }
    }

    public interface ICreateOrderCommandHandler { Task<Guid> Handle(CreateOrderCommand command, CancellationToken cancellationToken = default); }
    public interface IGetOrdersQueryHandler { Task<IReadOnlyCollection<OrderDto>> Handle(CancellationToken cancellationToken = default); }
    public interface IGetOrderByIdQueryHandler { Task<OrderDto> Handle(Guid orderId, CancellationToken cancellationToken = default); }

    public sealed class CreateOrderCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IUnitOfWork unitOfWork) : ICreateOrderCommandHandler
    {
        public async Task<Guid> Handle(CreateOrderCommand command, CancellationToken cancellationToken = default)
        {
            var userId = currentUserService.GetCurrentUserId();
            var cart = await context.Query<Domain.Entities.Cart>()
                .Include(x => x.Items).ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken)
                ?? throw new ValidationException("Cart is empty.");

            if (!cart.Items.Any())
            {
                throw new ValidationException("Cart is empty.");
            }

            var order = new Order
            {
                UserId = userId,
                ContactName = command.ContactName,
                ContactPhone = command.ContactPhone,
                DeliveryAddress = command.DeliveryAddress,
                DeliveryType = command.IsAsap ? DeliveryType.AsSoonAsPossible : DeliveryType.Scheduled,
                ScheduledDeliveryTimeUtc = command.IsAsap ? null : command.ScheduledDeliveryTimeUtc,
                TotalAmount = cart.Items.Sum(x => (x.Product?.Price ?? 0m) * x.Quantity),
                Items = cart.Items.Select(x => new OrderItem
                {
                    ProductId = x.ProductId,
                    ProductName = x.Product?.Name ?? string.Empty,
                    UnitPrice = x.Product?.Price ?? 0m,
                    Quantity = x.Quantity
                }).ToList()
            };

            await context.AddAsync(order, cancellationToken);
            foreach (var item in cart.Items.ToList())
            {
                context.Remove(item);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return order.Id;
        }
    }

    public sealed class GetOrdersQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IMapper mapper) : IGetOrdersQueryHandler
    {
        public async Task<IReadOnlyCollection<OrderDto>> Handle(CancellationToken cancellationToken = default)
        {
            var userId = currentUserService.GetCurrentUserId();
            return await context.Query<Order>()
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .ProjectTo<OrderDto>(mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);
        }
    }

    public sealed class GetOrderByIdQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IMapper mapper) : IGetOrderByIdQueryHandler
    {
        public async Task<OrderDto> Handle(Guid orderId, CancellationToken cancellationToken = default)
        {
            var userId = currentUserService.GetCurrentUserId();
            var order = await context.Query<Order>()
                .AsNoTracking()
                .Where(x => x.Id == orderId && x.UserId == userId)
                .ProjectTo<OrderDto>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(cancellationToken);

            return order ?? throw new NotFoundException($"Order with id '{orderId}' was not found.");
        }
    }
}

namespace FoodDelivery.Application
{
    using AutoMapper;
    using FoodDelivery.Application.Features.Auth;
    using FoodDelivery.Application.Features.Cart;
    using FoodDelivery.Application.Features.Menu;
    using FoodDelivery.Application.Features.Orders;
    using FoodDelivery.Domain.Entities;
    using FluentValidation;
    using Microsoft.Extensions.DependencyInjection;

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
}
