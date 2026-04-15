namespace FoodDelivery.WebApi.Controllers.V1;

using Asp.Versioning;
using FoodDelivery.Application.Features.Cart;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Authorize(Roles = "Customer,Admin")]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/cart")]
public class CartController(
    IGetCartQueryHandler getCartHandler,
    IAddCartItemCommandHandler addItemHandler,
    IUpdateCartItemCommandHandler updateItemHandler,
    IDeleteCartItemCommandHandler deleteItemHandler) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
        => Ok(await getCartHandler.Handle(cancellationToken));

    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddCartItemCommand command, CancellationToken cancellationToken)
    {
        await addItemHandler.Handle(command, cancellationToken);
        return NoContent();
    }

    [HttpPut("items/{id:guid}")]
    public async Task<IActionResult> UpdateItem(Guid id, [FromBody] UpdateCartItemCommand payload, CancellationToken cancellationToken)
    {
        await updateItemHandler.Handle(new UpdateCartItemCommand { CartItemId = id, Quantity = payload.Quantity }, cancellationToken);
        return NoContent();
    }

    [HttpDelete("items/{id:guid}")]
    public async Task<IActionResult> DeleteItem(Guid id, CancellationToken cancellationToken)
    {
        await deleteItemHandler.Handle(id, cancellationToken);
        return NoContent();
    }
}
