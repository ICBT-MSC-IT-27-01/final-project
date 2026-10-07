using System.Security.Claims;
using AnuradhapuraAI.Application.Authentication;
using AnuradhapuraAI.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnuradhapuraAI.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthenticationController(IAuthenticationService authenticationService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authenticationService.RegisterAsync(request, cancellationToken);

        if (result.Succeeded)
        {
            return CreatedAtAction(nameof(Register), result.Value);
        }

        return result.ErrorCode switch
        {
            AuthenticationErrorCodes.DuplicateEmail => Conflict(new { message = "Email is already registered." }),
            AuthenticationErrorCodes.RegisteredUserRoleMissing => StatusCode(
                StatusCodes.Status500InternalServerError,
                new { message = "Registration is temporarily unavailable." }),
            _ => BadRequest(new { message = "Registration failed." })
        };
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authenticationService.LoginAsync(request, cancellationToken);

        if (result.Succeeded)
        {
            return Ok(result.Value);
        }

        return result.ErrorCode switch
        {
            AuthenticationErrorCodes.InactiveUser => Forbid(),
            AuthenticationErrorCodes.InvalidCredentials => Unauthorized(new { message = "Invalid credentials." }),
            _ => Unauthorized(new { message = "Invalid credentials." })
        };
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<CurrentUserResponse> Me()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var role = User.FindFirstValue(ClaimTypes.Role);

        if (!int.TryParse(userIdClaim, out var userId)
            || string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(role))
        {
            return Unauthorized();
        }

        return Ok(new CurrentUserResponse(userId, email, role));
    }

    [Authorize(Policy = "AdministratorOnly")]
    [HttpGet("admin-check")]
    public IActionResult AdminCheck()
    {
        return Ok(new { role = ApprovedRoleNames.Administrator });
    }
}
