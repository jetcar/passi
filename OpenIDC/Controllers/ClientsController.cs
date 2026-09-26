using System;
using System.Linq;
using System.Security.Claims;
using ConfigurationManager;
using System.Threading.Tasks;
using GoogleTracer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIDC.Services;

namespace OpenIDC.Controllers
{
    /// <summary>
    /// Lets a signed-in Passi user manage the OAuth/OIDC clients they registered.
    /// Authenticated with the user's OpenIDC access token (Bearer); a user only ever sees their own clients.
    /// </summary>
    [ApiController]
    [Route("api/clients")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [IgnoreAntiforgeryToken]
    [Profile]
    public class ClientsController : ControllerBase
    {
        private readonly IRegisteredClientService _service;
        private readonly AppSetting _appSetting;

        public ClientsController(IRegisteredClientService service, AppSetting appSetting)
        {
            _service = service;
            _appSetting = appSetting;
        }

        /// <summary>
        /// Only tokens issued to first-party clients (the Passi website) may manage registrations; otherwise any
        /// third-party app a user signed in to could use that user's access token to add or delete their apps.
        /// </summary>
        private bool IsTrustedCaller()
        {
            var tokenClientId = User.FindFirst("client_id")?.Value;
            if (string.IsNullOrEmpty(tokenClientId))
                return false;

            var configured = _appSetting["ClientManagementClientIds"];
            var allowed = (string.IsNullOrWhiteSpace(configured) ? "SampleApp" : configured)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return allowed.Contains(tokenClientId, StringComparer.Ordinal);
        }

        private string Owner =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

        [HttpGet]
        public async Task<IActionResult> List() => await Run(async () => Ok(await _service.ListAsync(Owner)));

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ClientRegistrationRequest request) =>
            await Run(async () => Ok(await _service.CreateAsync(Owner, request)));

        [HttpPut("{clientId}")]
        public async Task<IActionResult> Update(string clientId, [FromBody] ClientUpdateRequest request) =>
            await Run(async () => OkOrNotFound(await _service.UpdateAsync(Owner, clientId, request)));

        [HttpPost("{clientId}/secret")]
        public async Task<IActionResult> RotateSecret(string clientId) =>
            await Run(async () => OkOrNotFound(await _service.RotateSecretAsync(Owner, clientId)));

        [HttpDelete("{clientId}")]
        public async Task<IActionResult> Delete(string clientId) =>
            await Run(async () => await _service.DeleteAsync(Owner, clientId) ? NoContent() : NotFound());

        private IActionResult OkOrNotFound(object value) => value == null ? NotFound() : Ok(value);

        private async Task<IActionResult> Run(Func<Task<IActionResult>> action)
        {
            if (!IsTrustedCaller())
                return Forbid();

            try
            {
                return await action();
            }
            catch (ClientRegistrationException e)
            {
                return BadRequest(new { errors = e.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
        }
    }
}
