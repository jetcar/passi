using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OpenIDC.Services;

namespace OpenIDC.Controllers;

[ApiController]
[IgnoreAntiforgeryToken]
public sealed class RegistrationController : ControllerBase
{
    private readonly DynamicClientRegistration _registration;
    private readonly ILogger<RegistrationController> _logger;

    public RegistrationController(DynamicClientRegistration registration, ILogger<RegistrationController> logger)
    {
        _registration = registration;
        _logger = logger;
    }

    [HttpPost("connect/register")]
    [Consumes("application/json")]
    [RequestSizeLimit(16384)]
    public IActionResult Register([FromBody] DynamicRegistrationRequest request)
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";
        try
        {
            if (!_registration.TryAcquire(HttpContext, out var retry))
            {
                Response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(retry.TotalSeconds)).ToString();
                return StatusCode(429, new { error = "temporarily_unavailable" });
            }
            var error = DynamicClientRegistration.Validate(request);
            if (error != null) return BadRequest(new { error });
            var client = _registration.Register(request);
            if (client == null) return StatusCode(503, new { error = "temporarily_unavailable" });
            return StatusCode(201, new
            {
                client_id = client.ClientId,
                client_id_issued_at = new DateTimeOffset(client.CreatedAt).ToUnixTimeSeconds(),
                client_id_expires_at = new DateTimeOffset(client.ExpiresAt.Value).ToUnixTimeSeconds(),
                client_name = client.DisplayName,
                redirect_uris = client.RedirectUris,
                token_endpoint_auth_method = "none",
                grant_types = client.GrantTypes,
                response_types = new[] { "code" },
                scope = string.Join(" ", client.AllowedScopes),
            });
        }
        catch (Exception)
        {
            // Exception text may contain serialized metadata; never log it.
            _logger.LogError("Dynamic client registration storage unavailable.");
            return StatusCode(503, new { error = "temporarily_unavailable" });
        }
    }
}
