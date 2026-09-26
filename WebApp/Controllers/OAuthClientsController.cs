using System;
using System.Text.Json;
using System.Threading.Tasks;
using ConfigurationManager;
using GoogleTracer;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using RestSharp;
using Services;

namespace WebApp.Controllers
{
    /// <summary>
    /// Backend for the "My OAuth apps" page. Forwards to OpenIDC's /api/clients using the signed-in user's
    /// OpenIDC access token, so OpenIDC scopes every operation to that user.
    /// </summary>
    [ApiController]
    [Route("api/oauthclients")]
    [Profile]
    public class OAuthClientsController : ControllerBase
    {
        private readonly IMyRestClient _restClient;
        private readonly IAntiforgery _antiforgery;
        private readonly string _openIdcUrl;

        public OAuthClientsController(IMyRestClient restClient, IAntiforgery antiforgery, AppSetting appSetting)
        {
            _restClient = restClient;
            _antiforgery = antiforgery;
            _openIdcUrl = (Environment.GetEnvironmentVariable("openIdcUrl") ?? appSetting["openIdcUrl"])?.TrimEnd('/');
        }

        /// <summary>Antiforgery token for the page's write requests, plus the discovery URL to show integrators.</summary>
        [HttpGet("context")]
        public IActionResult Context()
        {
            if (!User.Identity.IsAuthenticated)
                return Unauthorized();

            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
            return Ok(new
            {
                csrfToken = tokens.RequestToken,
                csrfHeaderName = tokens.HeaderName,
                discoveryUrl = $"{_openIdcUrl}/.well-known/openid-configuration",
            });
        }

        [HttpGet]
        public Task<IActionResult> List() => Forward(Method.Get, "");

        [HttpPost]
        public Task<IActionResult> Create([FromBody] JsonElement body) => Forward(Method.Post, "", body);

        [HttpPut("{clientId}")]
        public Task<IActionResult> Update(string clientId, [FromBody] JsonElement body) =>
            Forward(Method.Put, "/" + Uri.EscapeDataString(clientId), body);

        [HttpPost("{clientId}/secret")]
        public Task<IActionResult> RotateSecret(string clientId) =>
            Forward(Method.Post, "/" + Uri.EscapeDataString(clientId) + "/secret");

        [HttpDelete("{clientId}")]
        public Task<IActionResult> Delete(string clientId) =>
            Forward(Method.Delete, "/" + Uri.EscapeDataString(clientId));

        private async Task<IActionResult> Forward(Method method, string path, JsonElement? body = null)
        {
            if (!User.Identity.IsAuthenticated)
                return Unauthorized();

            var accessToken = await HttpContext.GetTokenAsync("access_token");
            if (string.IsNullOrEmpty(accessToken))
                return Unauthorized();

            var request = new RestRequest($"{_openIdcUrl}/api/clients{path}", method);
            request.AddHeader("Authorization", $"Bearer {accessToken}");
            if (body.HasValue)
                request.AddStringBody(body.Value.GetRawText(), DataFormat.Json);

            var response = await _restClient.ExecuteAsync(request);
            var status = (int)response.StatusCode;
            if (status == 0)
                return StatusCode(502, new { errors = "Identity service unavailable" });

            return new ContentResult
            {
                StatusCode = status,
                Content = response.Content,
                ContentType = "application/json",
            };
        }
    }
}
