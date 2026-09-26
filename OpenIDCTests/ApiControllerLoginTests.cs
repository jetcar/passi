using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ConfigurationManager;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using RestSharp;
using Services;
using WebApiDto.Auth;

namespace OpenIDCTests
{
    public class ApiControllerLoginTests
    {
        private CapturingRestClient _rest;

        private ApiController Controller()
        {
            _rest = new CapturingRestClient();
            var appSetting = new AppSetting(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { ["AppSetting:startRequest"] = "/api/auth/start" })
                .Build()) { PrefferAppsettingFile = true };

            return new ApiController(new FixedRandom(), _rest, appSetting, NullLogger<ApiController>.Instance, null, null);
        }

        [Test]
        public async Task LoginSendsTwoDigitNumberToPassiAndReturnsItToTheLoginPage()
        {
            var result = await Controller().Login("https://site/cb", "nonce", "alice@passi.cloud", "SampleApp");

            var sent = (StartLoginDto)_rest.Requests.Single().Parameters.OfType<BodyParameter>().Single().Value;
            Assert.That(sent.CheckNumber, Is.InRange(10, 99));

            var page = (CheckInputModel)((OkObjectResult)result).Value;
            Assert.That(page.CheckNumber, Is.EqualTo(sent.CheckNumber));
            Assert.That(page.CheckColor, Is.EqualTo(sent.CheckColor.ToString()), "color is still shown for older app versions");
        }

        [Test]
        public async Task LoginNumbersVaryAcrossLogins()
        {
            var numbers = new HashSet<int>();
            for (var i = 0; i < 40; i++)
            {
                var controller = Controller();
                await controller.Login("https://site/cb", "nonce", "alice@passi.cloud", "SampleApp");
                numbers.Add(((StartLoginDto)_rest.Requests.Single().Parameters.OfType<BodyParameter>().Single().Value).CheckNumber!.Value);
            }

            Assert.That(numbers.Count, Is.GreaterThan(5));
        }

        private class FixedRandom : IRandomGenerator
        {
            public string GetNumbersString(int i) => new string('1', i);
        }

        private class CapturingRestClient : IMyRestClient
        {
            public readonly List<RestRequest> Requests = new();

            public Task<RestResponse> ExecuteAsync(RestRequest request)
            {
                Requests.Add(request);
                return Task.FromResult(new RestResponse(request)
                {
                    StatusCode = HttpStatusCode.OK,
                    IsSuccessStatusCode = true,
                    ResponseStatus = ResponseStatus.Completed,
                    Content = $"{{\"SessionId\":\"{Guid.NewGuid()}\",\"RegisteredDevices\":[]}}",
                });
            }
        }
    }
}
