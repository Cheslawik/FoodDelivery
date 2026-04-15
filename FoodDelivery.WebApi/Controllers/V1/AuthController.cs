namespace FoodDelivery.WebApi.Controllers.V1;

using Asp.Versioning;
using FoodDelivery.Application.Features.Auth;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController(
    IRegisterUserCommandHandler registerUserCommandHandler,
    ILoginCommandHandler loginCommandHandler,
    IRefreshTokenCommandHandler refreshTokenCommandHandler,
    IRevokeRefreshTokenCommandHandler revokeRefreshTokenCommandHandler) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var fixedCommand = new RegisterUserCommand
        {
            Email = command.Email,
            FullName = command.FullName,
            Phone = command.Phone,
            Address = command.Address,
            Password = command.Password,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
        };

        return Ok(await registerUserCommandHandler.Handle(fixedCommand, cancellationToken));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
    {
        var fixedCommand = new LoginCommand
        {
            Email = command.Email,
            Password = command.Password,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
        };

        return Ok(await loginCommandHandler.Handle(fixedCommand, cancellationToken));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var fixedCommand = new RefreshTokenCommand
        {
            RefreshToken = command.RefreshToken,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
        };

        return Ok(await refreshTokenCommandHandler.Handle(fixedCommand, cancellationToken));
    }

    [AllowAnonymous]
    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RevokeRefreshTokenCommand command, CancellationToken cancellationToken)
        => Ok(await revokeRefreshTokenCommandHandler.Handle(command, cancellationToken));

    [Authorize(Roles = "Customer,Admin")]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
        var role = User.FindFirstValue(ClaimTypes.Role);
        var name = User.FindFirstValue(ClaimTypes.Name);

        return Ok(new { id, email, role, name });
    }
}
