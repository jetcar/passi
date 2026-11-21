using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.RateLimiting;
using WebApiDto.SmartHome;

namespace passi_webapi.SmartHome;

[AttributeUsage(AttributeTargets.Class)]
public sealed class HomeErrorsAttribute : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        if (context.Exception is not SmartHomeException error) return;
        context.Result = new ObjectResult(new { error = error.Code }) { StatusCode = error.StatusCode };
        context.ExceptionHandled = true;
    }
}

[ApiController, Route("api/smarthome"), HomeErrors]
// Header bearer authentication only; cookie-authenticated browser writes use the CSRF-protected WebApp BFF.
[IgnoreAntiforgeryToken]
[Authorize(Policy = SmartHomeRegistration.OwnerPolicy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[RequestSizeLimit(64 * 1024)]
public sealed class SmartHomeController(SmartHomeService homes) : ControllerBase
{
    private Guid Owner => Guid.Parse(User.FindFirst(SmartHomeRegistration.OwnerClaim).Value);

    [HttpGet("access")]
    public IActionResult Access() => Ok(new { allowed = true });

    [HttpGet]
    public async Task<IActionResult> Dashboard() => Ok(await homes.GetDashboardAsync(Owner));

    [HttpPost("homes")]
    public async Task<IActionResult> CreateHome(CreateHomeRequest request) =>
        Ok(await homes.CreateHomeAsync(Owner, request.Name));

    [HttpPost("homes/{homeId:guid}/enrollment")]
    public async Task<IActionResult> Reenroll(Guid homeId) => Ok(await homes.ReenrollAsync(Owner, homeId));

    [HttpPost("homes/{homeId:guid}/revoke")]
    public async Task<IActionResult> Revoke(Guid homeId)
    {
        await homes.RevokeAsync(Owner, homeId);
        return NoContent();
    }

    [HttpPost("homes/{homeId:guid}/devices/{deviceId}/power")]
    public async Task<IActionResult> Power(Guid homeId, string deviceId, PowerCommandRequest request) =>
        Ok(await homes.QueueCommandAsync(Owner, homeId, deviceId, request));
}

public sealed class CreateHomeRequest
{
    [Required, StringLength(80, MinimumLength = 1)] public string Name { get; set; }
}

public sealed class EnrollHomeRequest
{
    [Required, StringLength(256, MinimumLength = 32)] public string EnrollmentSecret { get; set; }
}

// Enrollment requires a one-use secret in the JSON body and does not use browser cookies.
[IgnoreAntiforgeryToken]
[ApiController, Route("api/smarthome/agent"), HomeErrors]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[RequestSizeLimit(4096)]
public sealed class HomeEnrollmentController(SmartHomeService homes) : ControllerBase
{
    [AllowAnonymous, HttpPost("homes/{homeId:guid}/enroll"), EnableRateLimiting("HomeEnrollment")]
    public async Task<IActionResult> Enroll(Guid homeId, EnrollHomeRequest request) =>
        Ok(await homes.EnrollAsync(homeId, request.EnrollmentSecret));
}
