// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Web;
using ConfigurationManager;
using IdentityModel;
using IdentityServer.Controllers.ViewInputs;
using IdentityServer.Controllers.ViewModels;
using IdentityServer4;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Repos.Migrations;
using RestSharp;
using Services;
using WebApiDto;
using WebApiDto.Auth;
using WebApiDto.Certificate;

namespace IdentityServer.Controllers.Account
{
    /// <summary>
    /// This sample controller implements a typical login/logout/provision workflow for local and external accounts.
    /// The login service encapsulates the interactions with the user data store. This data store is in-memory only and cannot be used for production!
    /// The interaction service provides a way for the UI to communicate with identityserver for validation and context retrieval
    /// </summary>
    [SecurityHeaders]
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IClientStore _clientStore;
        private readonly IAuthenticationSchemeProvider _schemeProvider;
        private readonly IEventService _events;
        private AppSetting _appSetting;
        private IMyRestClient _myRestClient;
        public AccountController(
            IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            IAuthenticationSchemeProvider schemeProvider,
            IEventService events, AppSetting appSetting, IMyRestClient myRestClient)
        {
            // if the TestUserStore is not in DI, then we'll just use the global users collection
            // this is where you would plug in your own custom identity management library (e.g. ASP.NET Identity)


            _interaction = interaction;
            _clientStore = clientStore;
            _schemeProvider = schemeProvider;
            _events = events;
            _appSetting = appSetting;
            _myRestClient = myRestClient;
        }


        [HttpGet]
        public async Task<LoginViewModel> ApiLogin([FromQuery] string returnUrl)
        {
            // build a model so we know what to show on the login page
            LoginViewModel vm = new LoginViewModel()
            {
                Username = ""
            };

            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
            if (context?.IdP != null && await _schemeProvider.GetSchemeAsync(context.IdP) != null)
            {
                // this is meant to short circuit the UI and only trigger the one external IdP
                vm.ReturnUrl = returnUrl;
            }

            if (context?.Client.ClientId == null)
            {
                ModelState.TryAddModelError("invalid client", "invalid client");
            }
            else
            {
                vm.Nonce = context.Parameters["nonce"];

                var client = await _clientStore.FindEnabledClientByIdAsync(context.Client.ClientId);
                if (client == null)
                {
                    ModelState.TryAddModelError("invalid client", "invalid client");
                }
            }

            return vm;
        }

        /// <summary>
        /// Entry point into the login workflow
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Login([FromQuery] string returnUrl)
        {
            // build a model so we know what to show on the login page
            LoginViewModel vm = new LoginViewModel()
            {
                Username = ""
            };

            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
            if (context?.IdP != null && await _schemeProvider.GetSchemeAsync(context.IdP) != null)
            {
                // this is meant to short circuit the UI and only trigger the one external IdP
                vm.ReturnUrl = returnUrl;
            }

            if (context?.Client.ClientId == null)
            {
                ModelState.TryAddModelError("invalid client", "invalid client");
            }
            else
            {
                vm.Nonce = context.Parameters["nonce"];

                var client = await _clientStore.FindEnabledClientByIdAsync(context.Client.ClientId);
                if (client == null)
                {
                    ModelState.TryAddModelError("invalid client", "invalid client");
                }
            }

            return View(vm);
        }

        /// <summary>
        /// Handle postback from username/password login
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginInputModel model)
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

            var context = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);
            
            var request = new RestRequest(_appSetting["startRequest"], Method.Post);
            var possibleCodes = new List<Color>()
            {
                Color.blue,
                Color.green,
                Color.red,
                Color.yellow
            };
            var redirect_uri = model.ReturnUrl.Split('&').Select(x =>
            {
                var values = x.Split('=').Select(a => HttpUtility.UrlDecode(a)).ToArray();
                if (values.Length >= 2)
                    return new { Key = values[0], Value = values[1] };
                return new { Key = "redirect_uri", Value = model.ReturnUrl };
            }).Where(x => x.Key == "redirect_uri").Select(x => x.Value).FirstOrDefault();

            var index = new Random().Next(0, possibleCodes.Count - 1);
            var startLoginDto = new StartLoginDto()
            {
                Username = model.Username,
                ClientId = context?.Client.ClientId,
                ReturnUrl = redirect_uri,
                CheckColor = possibleCodes[index],
                RandomString = model.Nonce
            };
            request.AddJsonBody(startLoginDto);
            var result = _myRestClient.ExecuteAsync(request).Result;
            if (result.IsSuccessful)
            {
                var loginResponceDto = JsonConvert.DeserializeObject<LoginResponceDto>(result.Content);
                var color = startLoginDto.CheckColor.ToString();
                return RedirectToAction("Check", new
                {
                    sessionId = loginResponceDto.SessionId,
                    returnUrl = model.ReturnUrl,
                    username = model.Username,
                    color = color,
                    randomString = startLoginDto.RandomString
                });
            }
            else
            {
                //var errorResult = JsonConvert.DeserializeObject<ApiResponse<string>>(result.Content);
                ModelState.AddModelError(result.Content, result.ErrorMessage);
                //ModelState.AddModelError(errorResult.Message, errorResult.Message);
            }
            return View(new LoginViewModel()
            {
                ReturnUrl = model.ReturnUrl,
                Username = model.Username,
                RememberLogin = model.RememberLogin,
            });
        }

        [HttpGet]
        public async Task<IActionResult> Check(Guid sessionId, string returnUrl, string username, string color, string randomString)
        {
            var vm = await BuildLoginViewModelAsync(new CheckViewModel()
            {
                CheckColor = color,
                ReturnUrl = returnUrl,
                SessionId = sessionId,
                Username = username,
                RandomString = randomString
            });
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Check(CheckInputModel model)
        {
            // check if we are in the context of an authorization request
            var context = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);
            bool needRefresh = true;

            var request = new RestRequest(_appSetting["checkRequest"] + "?sessionId=" + model.SessionId, Method.Get);
            var result = _myRestClient.ExecuteAsync(request).Result;

            if (result.IsSuccessful)
            {
                var checkResponceDto = JsonConvert.DeserializeObject<CheckResponceDto>(result.Content);
                await _events.RaiseAsync(new UserLoginSuccessEvent(model.Username, model.Username, model.Username,
                    clientId: context?.Client.ClientId));
                var isuser = new IdentityServerUser(model.Username)
                {
                    DisplayName = model.Username,
                };
                if (checkResponceDto.PublicCertThumbprint != null)
                {
                    var request2 =
                        new RestRequest($"api/Certificate/Public?thumbprint={checkResponceDto.PublicCertThumbprint}&username={checkResponceDto.Username}",
                            Method.Get);
                    var result2 = _myRestClient.ExecuteAsync(request2).Result;
                    if (result2.IsSuccessful)
                    {
                        var cert = JsonConvert.DeserializeObject<CertificateDto>(result2.Content);
                        var publicCertificate = new X509Certificate2(Convert.FromBase64String(cert.PublicCert));
                        if (publicCertificate.NotAfter < DateTime.UtcNow && publicCertificate.NotBefore > DateTime.UtcNow)
                        {
                            ModelState.AddModelError("Error", "Invalid Certificate");

                        }
                        var nonce = model.RandomString;

                        // ComputeHash - returns byte array
                        var isValid = CertHelper.VerifyData(nonce, checkResponceDto.SignedHash, cert.PublicCert);
                        if (isValid)
                        {
                            isuser.AdditionalClaims.Add(new Claim("Thumbprint", checkResponceDto.PublicCertThumbprint));
                            isuser.AdditionalClaims.Add(new Claim("SignedHash", checkResponceDto.SignedHash));
                            await AuthenticationManagerExtensions.SignInAsync(HttpContext, isuser,
                                new AuthenticationProperties()
                                {
                                    IsPersistent = true,
                                    AllowRefresh = true,
                                    ExpiresUtc =
                                        DateTimeOffset.UtcNow.AddMinutes(Convert.ToDouble(_appSetting["SessionLength"]))
                                });
                            return RedirectPermanent(model.ReturnUrl);
                        }
                        else
                        {
                            ModelState.AddModelError("Error", "Invalid Signature");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("Error", result2.Content  + " " + result2.ErrorMessage);
                    }
                }
            }

            if (!result.Content.Contains("Waiting for response"))
            {
                var errorResult = JsonConvert.DeserializeObject<ApiResponseDto<string>>(result.Content);
                ModelState.AddModelError(errorResult.Message, errorResult.Message);
                needRefresh = false;
            }

            // something went wrong, show form with error
            var vm = await BuildLoginViewModelAsync(new CheckViewModel()
            {
                CheckColor = model.CheckColor,
                ReturnUrl = model.ReturnUrl,
                SessionId = model.SessionId,
                Username = model.Username,
                RememberLogin = model.RememberLogin,
                NeedRefresh = needRefresh,
            });
            return View(vm);
        }

        [HttpPost]
        public IActionResult CancelLogin(LoginInputModel model)
        {
            if (model.ReturnUrl == "/diagnostics")
            {
                return RedirectPermanent("/");
            }
            var redirect_uri = model.ReturnUrl.Split('&').Select(x =>
            {
                var values = x.Split('=').Select(a => HttpUtility.UrlDecode(a)).ToArray();
                return new { Key = values[0], Value = values[1] };
            }).Where(x => x.Key == "redirect_uri").Select(x => x.Value).FirstOrDefault();
            var host = new Uri(redirect_uri);
            var hostScheme = host.Scheme + "://" + host.Host + ":" + host.Port;
            return RedirectPermanent(hostScheme);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(LoginInputModel model)
        {
            // check if we are in the context of an authorization request
            var context = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);

            if (context != null)
            {
                // if the user cancels, send a result back into IdentityServer as if they
                // denied the consent (even if this client does not require consent).
                // this will send back an access denied OIDC error response to the client.
                await _interaction.DenyAuthorizationAsync(context, AuthorizationError.AccessDenied);

                // we can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
                if (context.IsNativeClient())
                {
                    // The client is native, so this change in how to
                    // return the response is for better UX for the end user.
                    return this.LoadingPage("Redirect", model.ReturnUrl);
                }

                return Redirect(model.ReturnUrl);
            }
            else
            {
                // since we don't have a valid context, then we just go back to the home page
                return Redirect("~/");
            }
        }

        [HttpGet]
        public async Task<IActionResult> BackChannelLogout(string post_logout_redirect_uri, string state, string id_token_hint)
        {
            return Redirect($"{post_logout_redirect_uri}?state={state}");
        }

        /// <summary>
        /// Show logout page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Logout(string logoutId)
        {
            // build a model so the logout page knows what to display
            var vm = await BuildLogoutViewModelAsync(logoutId);

            if (vm.ShowLogoutPrompt == false)
            {
                // if the request for logout was properly authenticated from IdentityServer, then
                // we don't need to show the prompt and can just log the user out directly.
                return await Logout(vm);
            }

            return View(vm);
        }

        /// <summary>
        /// Handle logout page postback
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(LogoutInputModel model)
        {
            // build a model so the logged out page knows what to display
            var vm = await BuildLoggedOutViewModelAsync(model.LogoutId);

            if (User?.Identity.IsAuthenticated == true)
            {
                // delete local authentication cookie
                await HttpContext.SignOutAsync();

                // raise the logout event
                await _events.RaiseAsync(new UserLogoutSuccessEvent(User.GetSubjectId(), User.GetDisplayName()));
            }

            // check if we need to trigger sign-out at an upstream identity provider
            if (vm.TriggerExternalSignout)
            {
                // build a return URL so the upstream provider will redirect back
                // to us after the user has logged out. this allows us to then
                // complete our single sign-out processing.
                string url = Url.Action("Logout", new { logoutId = vm.LogoutId });

                // this triggers a redirect to the external provider for sign-out
                return SignOut(new AuthenticationProperties { RedirectUri = url }, vm.ExternalAuthenticationScheme);
            }

            return View("LoggedOut", vm);
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        /*****************************************/
        /* helper APIs for the AccountController */
        /*****************************************/

        private async Task<CheckViewModel> BuildLoginViewModelAsync(CheckViewModel model)
        {
            var context = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);

            if (context?.Client.ClientId == null)
            {
                ModelState.AddModelError("Client not found", "Client not found");
            }
            if (context?.Client.ClientId != null)
            {
                var client = await _clientStore.FindEnabledClientByIdAsync(context.Client.ClientId);
                if (client == null)
                {
                    ModelState.AddModelError("Client not found", "Client not found");
                }
            }

            model.Username = context?.LoginHint;

            return model;
        }

        private async Task<LogoutViewModel> BuildLogoutViewModelAsync(string logoutId)
        {
            var vm = new LogoutViewModel { LogoutId = logoutId, ShowLogoutPrompt = true };

            if (User?.Identity.IsAuthenticated != true)
            {
                // if the user is not authenticated, then just show logged out page
                vm.ShowLogoutPrompt = false;
                return vm;
            }

            var context = await _interaction.GetLogoutContextAsync(logoutId);
            if (context?.ShowSignoutPrompt == false)
            {
                // it's safe to automatically sign-out
                vm.ShowLogoutPrompt = false;
                return vm;
            }

            // show the logout prompt. this prevents attacks where the user
            // is automatically signed out by another malicious web page.
            return vm;
        }

        private async Task<LoggedOutViewModel> BuildLoggedOutViewModelAsync(string logoutId)
        {
            // get context information (client name, post logout redirect URI and iframe for federated signout)
            var logout = await _interaction.GetLogoutContextAsync(logoutId);

            var vm = new LoggedOutViewModel
            {
                AutomaticRedirectAfterSignOut = true,
                PostLogoutRedirectUri = logout?.PostLogoutRedirectUri,
                ClientName = string.IsNullOrEmpty(logout?.ClientName) ? logout?.ClientId : logout?.ClientName,
                SignOutIframeUrl = logout?.SignOutIFrameUrl,
                LogoutId = logoutId
            };

            if (User?.Identity.IsAuthenticated == true)
            {
                var idp = User.FindFirst(JwtClaimTypes.IdentityProvider)?.Value;
                if (idp != null && idp != IdentityServer4.IdentityServerConstants.LocalIdentityProvider)
                {
                    var providerSupportsSignout = await HttpContext.GetSchemeSupportsSignOutAsync(idp);
                    if (providerSupportsSignout)
                    {
                        if (vm.LogoutId == null)
                        {
                            // if there's no current logout context, we need to create one
                            // this captures necessary info from the current logged in user
                            // before we signout and redirect away to the external IdP for signout
                            vm.LogoutId = await _interaction.CreateLogoutContextAsync();
                        }

                        vm.ExternalAuthenticationScheme = idp;
                    }
                }
            }

            return vm;
        }
    }
}