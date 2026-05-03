using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhPayrollTimeApi.Api.Services;

namespace PhPayrollTimeApi.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController : ControllerBase
{
    [HttpPost("token")]
    [AllowAnonymous]
    public IActionResult GenerateToken(
        [FromBody] TokenRequest request,
        [FromServices] IWebHostEnvironment env,
        [FromServices] ITestTokenService? tokenService)
    {
        if (!env.IsDevelopment() || tokenService is null)
            return NotFound();

        var token = tokenService.GenerateToken(request.Sub, request.Role);
        return Ok(new { token });
    }
}

public record TokenRequest(string Sub, string Role);
