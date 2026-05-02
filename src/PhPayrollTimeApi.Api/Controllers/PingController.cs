using Asp.Versioning;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PhPayrollTimeApi.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ping")]
[Authorize]
public class PingController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        sub = User.FindFirstValue("sub"),
        role = User.FindFirstValue("role")
    });
}
