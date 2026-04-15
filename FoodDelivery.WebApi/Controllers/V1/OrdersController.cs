namespace FoodDelivery.WebApi.Controllers.V1;

using Asp.Versioning;
using FoodDelivery.Application.Features.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Authorize(Roles = "Customer,Admin")]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/orders")]
public class OrdersController(
    ICreateOrderCommandHandler createOrderHandler,
    IGetOrdersQueryHandler getOrdersHandler,
    IGetOrderByIdQueryHandler getOrderByIdHandler) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var orderId = await createOrderHandler.Handle(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = orderId, version = "1" }, new { orderId });
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
        => Ok(await getOrdersHandler.Handle(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await getOrderByIdHandler.Handle(id, cancellationToken));
}
