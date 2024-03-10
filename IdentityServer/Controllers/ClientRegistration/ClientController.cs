using ConfigurationManager;
using IdentityModel;
using IdentityServer.services;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System;
using System.Collections.Generic;
using System.Linq;
using IdentityServer4.Storage.Models;

namespace IdentityServer.Controllers.ClientRegistration
{
    //[SecurityHeaders]
    [Authorize]
    [GoogleTracer.Profile]
    public class ClientController : Controller
    {
        private IIdentityClientsRepository _clients;
        private AppSetting _appSetting;

        public ClientController(IIdentityClientsRepository clients, AppSetting appSetting)
        {
            _clients = clients;
            _appSetting = appSetting;
        }

        public IActionResult Cancel()
        {
            return RedirectToAction("Index");
        }

        public IActionResult Index()
        {
            var clients = _clients.GetUserRegisteredClients(User.GetSubjectId());
            string adminAccount = _appSetting["adminAccount"];
            if (!string.IsNullOrEmpty(adminAccount) && adminAccount == User.GetSubjectId())
            {
                clients.AddRange(_clients.GetAllClients());
            }
            var view = new ClientView();
            foreach (var client in clients)
            {
                view.ClientItems.Add(new ClientItem()
                {
                    Id = client.Id,
                    Name = client.ClientId,
                    Url = client.ClientUri,
                    Enabled = client.Enabled ? "Enabled" : "Disabled",
                    CreatedTime = client.Created
                });
            }
            return View(view);
        }

        public IActionResult Authorize([FromRoute] string route)
        {
            return RedirectToAction("Index");
        }

        public IActionResult Details(int id)
        {
            var client = _clients.GetUserRegisteredClientsById(id, User.GetSubjectId());
            if (client == null)
            {
                ModelState.TryAddModelError("ClientId", "Client not found.");
                return View(new ClientItemInput());
            }
            var clientView = new ClientItemInput()
            {
                Id = client.Id,
                ClientId = client.ClientId,
                Url = client.ClientUri,
                Enabled = client.Enabled,
                ReturnUrl = client.RedirectUris == null ? null : string.Join(",\r\n ", client.RedirectUris.Select(x => x.RedirectUri)),
                ReturnUrlCount = client.RedirectUris?.Count ?? 0
            };
            return View(clientView);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Details(ClientItemInput model)
        {
            model.ReturnUrlCount = model.ReturnUrl?.Split(',')?.Count() ?? 0;

            if (!ModelState.IsValid)
                return View(model);

            var exists = _clients.Exists(model.Id, User.GetSubjectId());
            if (!exists)
            {
                ModelState.TryAddModelError("ClientId", "Client not found.");
                return View(model);
            }

            _clients.Update(model.Id, User.GetSubjectId(), model.ClientSecret, model.ReturnUrl, model.Url);

            return RedirectToAction("Index");
        }

        public IActionResult AddProject()
        {
            return RedirectToAction("Create");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ClientItemInput model)
        {
            if (!ModelState.IsValid)
                return View(model);
            if (_clients.IsClientIdTaken(model.ClientId))
            {
                ModelState.TryAddModelError("ClientId", "Client Id is taken");
                return View(model);
            }

            var client = new Client()
            {
                ClientId = model.ClientId,
                ClientSecrets = new List<Secret>() { new Secret() { Value = model.ClientSecret.ToSha256() } },
                RedirectUris = new List<string>() { model.Url },
                RequirePkce = false,
                AllowedGrantTypes = GrantTypes.CodeAndClientCredentials,
                AllowedScopes = new List<string>() { "profile", "openid", "email" },
                AlwaysIncludeUserClaimsInIdToken = true,
                UserSsoLifetime = 10,
                ClientUri = model.Url,
                AccessTokenLifetime = 1,
                AlwaysSendClientClaims = true,
            };
            var savedClient = _clients.AddClient(client, User.GetSubjectId());

            return RedirectToAction("Details", new { id = savedClient.Id });
        }

        public IActionResult Create()
        {
            var model = new ClientItemInput()
            {
                ClientSecret = Guid.NewGuid().ToString()
            };
            return View(model);
        }
    }
}