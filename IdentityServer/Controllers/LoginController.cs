using System.Threading.Tasks;
using IdentityServer.Controllers.ViewModels;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostSharp.Extensibility;

namespace IdentityServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    [Profile(AttributeTargetElements = MulticastTargets.Method)]
    public class LoginController : ControllerBase
    {
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IAuthenticationSchemeProvider _schemeProvider;
        private readonly IClientStore _clientStore;

        public LoginController(IIdentityServerInteractionService interaction, IAuthenticationSchemeProvider schemeProvider, IClientStore clientStore)
        {
            _interaction = interaction;
            _schemeProvider = schemeProvider;
            _clientStore = clientStore;
        }

        [HttpPost]
        public async Task<IActionResult> Start([FromBody] LoginRequestDto loginRequest)
        {
            // build a model so we know what to show on the login page
            LoginViewModel vm = new LoginViewModel()
            {
                Username = ""
            };

            var context = await _interaction.GetAuthorizationContextAsync(loginRequest.returnUrl);
            if (context?.IdP != null && await _schemeProvider.GetSchemeAsync(context.IdP) != null)
            {
                // this is meant to short circuit the UI and only trigger the one external IdP
                vm.ReturnUrl = loginRequest.returnUrl;
            }

            if (context?.Client.ClientId == null)
            {
                ModelState.TryAddModelError("clientId", "invalid client");
            }
            else
            {
                vm.Nonce = context.Parameters["nonce"];

                var client = await _clientStore.FindEnabledClientByIdAsync(context.Client.ClientId);
                if (client == null)
                {
                    ModelState.TryAddModelError("clientId", "invalid client");
                }
            }

            if (!ModelState.IsValid)
            {

                return BadRequest(ModelState);

            }

            return Ok(vm);
        }
    }

    public class LoginRequestDto
    {
        public string returnUrl { get; set; }
    }
}
