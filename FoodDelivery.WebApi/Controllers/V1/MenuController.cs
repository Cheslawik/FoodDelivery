namespace FoodDelivery.WebApi.Controllers.V1;

using Asp.Versioning;
using FoodDelivery.Application.Features.Menu;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/menu")]
public class MenuController(IGetMenuQueryHandler menuHandler, IGetProductByIdQueryHandler productHandler) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] GetMenuQuery query, CancellationToken cancellationToken)
        => Ok(await menuHandler.Handle(query, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await productHandler.Handle(id, cancellationToken));
}
