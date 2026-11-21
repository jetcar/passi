using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using WebApp.Services;

namespace WebApp.Controllers;

[ApiController, Route("api/smarthome")]
[AutoValidateAntiforgeryToken]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[RequestSizeLimit(4096)]
public sealed class SmartHomeController(SmartHomeClient client, IAntiforgery antiforgery) : ControllerBase
{
    [HttpGet("csrf")]
    public async Task<IActionResult> Csrf()
    {
        var access = await Forward(HttpMethod.Get, "access");
        if (access is not ContentResult { StatusCode: 200 }) return access;
        return Ok(new { token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });
    }

    [HttpGet("access")]
    public Task<IActionResult> Access() => Forward(HttpMethod.Get, "access");

    [HttpGet]
    public Task<IActionResult> Dashboard() => Forward(HttpMethod.Get, "");

    [HttpPost("homes")]
    public Task<IActionResult> CreateHome([FromBody] JsonElement body) => Forward(HttpMethod.Post, "homes", body);

    [HttpPost("homes/{homeId:guid}/enrollment")]
    public Task<IActionResult> Reenroll(Guid homeId) => Forward(HttpMethod.Post, $"homes/{homeId}/enrollment");

    [HttpPost("homes/{homeId:guid}/revoke")]
    public Task<IActionResult> Revoke(Guid homeId) => Forward(HttpMethod.Post, $"homes/{homeId}/revoke");

    [HttpPost("homes/{homeId:guid}/devices/{deviceId}/power")]
    public Task<IActionResult> Power(Guid homeId, string deviceId, [FromBody] JsonElement body) =>
        Forward(HttpMethod.Post, $"homes/{homeId}/devices/{Uri.EscapeDataString(deviceId)}/power", body);

    private async Task<IActionResult> Forward(HttpMethod method, string path, JsonElement? body = null)
    {
        Response.Headers.CacheControl = "no-store";
        if (User.Identity?.IsAuthenticated != true) return Unauthorized();
        var token = await HttpContext.GetTokenAsync("Cookies", "access_token");
        if (string.IsNullOrEmpty(token)) return Unauthorized();
        try
        {
            using var response = await client.SendAsync(method, path, token, body, HttpContext.RequestAborted);
            // Redirects are never followed; their content and Location are not relayed to the browser.
            if ((int)response.StatusCode is >= 300 and < 400)
                return StatusCode(502, new { error = "home_service_unavailable" });
            return new ContentResult {
                StatusCode = (int)response.StatusCode,
                Content = await response.Content.ReadAsStringAsync(HttpContext.RequestAborted),
                ContentType = "application/json"
            };
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            // A timeout says nothing about whether a write committed. Neither side retries it here.
            return StatusCode(502, new { error = "home_service_unavailable" });
        }
    }
}
