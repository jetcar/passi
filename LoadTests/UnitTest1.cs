using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using AppConfig;
using MauiViewModels.utils.Services.Certificate;
using Microsoft.Extensions.Configuration;
using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using Newtonsoft.Json;
using RestSharp;
using YamlDotNet.Core.Tokens;
using static Abstracta.JmeterDsl.JmeterDsl;

namespace LoadTests
{
    public class Tests
    {
        private IConfigurationRoot _configuration;
        private static RestClient _client;

        [SetUp]
        public void Setup()
        {
            _configuration = new ConfigurationBuilder().AddJsonFile("test.appsettings.json").Build();
            ServicePointManager
                    .ServerCertificateValidationCallback +=
                (sender, cert, chain, sslPolicyErrors) => true;

            _client = new RestClient()
            {
            };
        }

        [Test]
        public void IndexPage()
        {
            var scenario = Scenario.Create("http_scenario", async context =>
            {
                var request =
                    new RestRequest(_configuration["PassiWebAppUrl"]);
                // .WithBody(new StringContent("{ some JSON }", Encoding.UTF8, "application/json"));

                var response = await _client.ExecuteGetAsync(request);
                if (response.IsSuccessful)
                    return Response.Ok();
                return Response.Fail();
            }).WithLoadSimulations(
                // it creates 100 copies and keeps them running
                // until the iterations count reaches 1000
                Simulation.Inject(500, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1))
            //Simulation.IterationsForInject(rate: 10000,
            //    interval: TimeSpan.FromSeconds(10),
            //    iterations: 2000)
            );

            var stats = NBomberRunner
                .RegisterScenarios(scenario)
                .WithReportFolder("report")
                .Run();
            Console.WriteLine("e");
            Assert.That(stats.ScenarioStats[0].Ok.Request.RPS, Is.GreaterThan(999));
            Assert.That(stats.AllFailCount == 0);
            Assert.That(stats.ScenarioStats[0].Ok.Latency.Percent99, Is.LessThan(10000));
        }

        [Test]
        public void LoginRequest()
        {
            var random = new Random();
            var certHelper = new CertHelper(null);
            var scenario = Scenario.Create("http_scenario", async context =>
                    {
                        var index = random.Next(10, 9000);
                        var number = index.ToString();
                        var username = $"test{number}@passi.cloud";
                        var deviceGuid = "";
                        var accountGuid = "";
                        var certThumbprint = "";
                        X509Certificate2 cert = null;
                        using (var reader = new StreamReader($"../../../../certs/admin{number}.crt"))
                        {
                            var strings = reader.ReadLine().Split(";");
                            accountGuid = strings[1];
                            deviceGuid = strings[2];
                            certThumbprint = strings[3];
                            cert = new X509Certificate2(Convert.FromBase64String(strings[5]), strings[4]);
                        }
                        var signedHash = Guid.NewGuid().ToString();
                        var step1 = await Step.Run<LoginResult>("step_1_login_start", context, async () =>
                        {
                            var loginResp = Login(username, out var returnUrl, out var nonce);
                            var value = loginResp.Content;
                            if (!loginResp.IsSuccessful || string.IsNullOrEmpty(value))
                                return Response.Fail(payload: new LoginResult());
                            var sessionId = JsonConvert.DeserializeObject<dynamic>(value).sessionId;
                            return Response.Ok(payload: new LoginResult()
                            {
                                returnUrl = returnUrl,
                                nonce = nonce,
                                sessionId = sessionId.ToString(),
                                username = username
                            });
                        });

                        var step2 = await Step.Run("step_2_check", context, async () =>
                        {
                            var resp = await Check(step1.Payload.Value.nonce, username, step1.Payload.Value.sessionId, step1.Payload.Value.returnUrl);

                            var value = resp.Content;
                            if (!resp.IsSuccessful || string.IsNullOrEmpty(value))
                                return Response.Fail();
                            return Response.Ok();
                        });
                        var step3 = await Step.Run("step_3_sync_accounts", context, async () =>
                        {
                            var resp = await MobileSync(deviceGuid, accountGuid);

                            var value = resp.Content;
                            if (!resp.IsSuccessful || string.IsNullOrEmpty(value))
                                return Response.Fail();
                            return Response.Ok();
                        });
                        var step4 = await Step.Run("step_4_poll_sessions", context, async () =>
                        {
                            string value = "";
                            do
                            {
                                var resp = await PollSessions(deviceGuid, accountGuid);

                                value = resp.Content;
                                if (!resp.IsSuccessful)
                                    return Response.Fail();
                            } while (string.IsNullOrEmpty(value));

                            return Response.Ok();
                        });

                        var step5 = await Step.Run("step_5_send_signed_data", context, async () =>
                        {
                            signedHash = certHelper.GetSignedData(step1.Payload.Value.nonce, cert);
                            var resp = await SendSignedHash(step1.Payload.Value.sessionId, certThumbprint, signedHash);

                            var value = resp.Content;
                            if (!resp.IsSuccessful || string.IsNullOrEmpty(value))
                                return Response.Fail();
                            return Response.Ok();
                        });

                        var step6 = await Step.Run("step_6_check_after_Signing", context, async () =>
                        {
                            var resp = await Check(step1.Payload.Value.nonce, username, step1.Payload.Value.sessionId, step1.Payload.Value.returnUrl);

                            var value = resp.Content;
                            if (!resp.IsSuccessful || string.IsNullOrEmpty(value))
                                return Response.Fail();
                            var redirect = JsonConvert.DeserializeObject<dynamic>(value).redirect.ToString();
                            var redirectResponse = await Redirect(redirect, resp.Cookies);
                            var value2 = redirectResponse.Content;
                            if (!redirectResponse.IsSuccessful || string.IsNullOrEmpty(value2))
                                return Response.Fail();

                            return Response.Ok();
                        });

                        return Response.Ok();
                    })
                .WithLoadSimulations(
                Simulation.Inject(100, TimeSpan.FromSeconds(20), TimeSpan.FromMinutes(5))
            );

            var stats = NBomberRunner
                .RegisterScenarios(scenario)
                .WithReportFolder("report")
                .Run();

            Assert.That(stats.ScenarioStats[0].Ok.Request.RPS, Is.GreaterThan(1d));
            Assert.That(stats.AllFailCount == 0);
        }

        private async Task<RestResponse> SendSignedHash(string sessionId, string certThumbprint, string signedHash)
        {
            var syncAccountsDto = new AuthorizeDto()
            {
                SessionId = sessionId,
                PublicCertThumbprint = certThumbprint,
                SignedHash = signedHash
            };
            var request = new RestRequest(_configuration["PassiApiUrl"] + ConfigSettings.Authorize);
            request.AddJsonBody(syncAccountsDto);

            var response = await _client.ExecutePostAsync(request);

            return response;
        }

        private async Task<RestResponse> PollSessions(string deviceGuid, string accountGuid)
        {
            var syncAccountsDto = new SyncAccountsDto()
            {
                DeviceId = deviceGuid,
                Guids = new List<string>() { accountGuid }
            };
            var request = new RestRequest(_configuration["PassiApiUrl"] + ConfigSettings.CheckForStartedSessions);
            request.AddJsonBody(syncAccountsDto);

            var response = await _client.ExecutePostAsync(request);

            return response;
        }

        private async Task<RestResponse> MobileSync(string deviceGuid, string accountGuid)
        {
            var syncAccountsDto = new SyncAccountsDto()
            {
                DeviceId = deviceGuid,
                Guids = new List<string>() { accountGuid }
            };
            var request =
                new RestRequest(_configuration["PassiApiUrl"] + ConfigSettings.SyncAccounts);
            request.AddJsonBody(syncAccountsDto);

            var response = await _client.ExecutePostAsync(request);

            return response;
        }

        private async Task<RestResponse> Check(string nonce, string username, string sessionId, string returnUrl)
        {
            var checkDto = new CheckDto()
            {
                checkColor = "green",
                randomString = nonce,
                returnUrl = returnUrl,
                sessionId = sessionId,

                username = username,
            };
            var request =
                new RestRequest(_configuration["IdentityUrl"] + "/api/check");
            request.AddJsonBody(checkDto);

            var response = await _client.ExecutePostAsync(request);

            return response;
        }

        private async Task<RestResponse> Redirect(string returnUrl, CookieCollection cookies)
        {
            var request = new RestRequest(_configuration["IdentityUrl"] + returnUrl);
            foreach (Cookie cookie in cookies)
            {
                request.AddCookie(cookie.Name, cookie.Value, cookie.Path, cookie.Domain);
            }
            var response = await _client.ExecuteGetAsync(request);

            return response;
        }

        private RestResponse Login(string username, out string returnUrl, out string nonce)
        {
            returnUrl = "";
            nonce = "";
            var loginstart = new RestRequest(_configuration["PassiWebAppUrl"] + "/Auth/Login");
            var loginStartResponse = _client.ExecuteGet(loginstart);
            if (!loginStartResponse.IsSuccessful)
                return loginStartResponse;
            var strings = loginStartResponse.ResponseUri.Query.Split("?").Where(x => !string.IsNullOrEmpty(x)).ToList();
            var firstOrDefault = strings.FirstOrDefault(x => x != null && x.StartsWith("ReturnUrl="));
            var redirectUri = firstOrDefault?.Replace("ReturnUrl=", "");
            if (string.IsNullOrEmpty(redirectUri))
            {
                var url = loginStartResponse.ResponseUri.ToString();
                loginStartResponse = _client.ExecuteGet(new RestRequest(url));
                strings = loginStartResponse.ResponseUri.Query.Split("?").Where(x => !string.IsNullOrEmpty(x)).ToList();
                firstOrDefault = strings.FirstOrDefault(x => x != null && x.StartsWith("ReturnUrl="));
                redirectUri = firstOrDefault?.Replace("ReturnUrl=", "");
            }

            returnUrl = HttpUtility.UrlDecode(redirectUri);
            nonce = returnUrl.Split("&").FirstOrDefault(x => x.StartsWith("nonce="))?.Replace("nonce=", "");
            var loginDto = new LoginDto()
            {
                ReturnUrl = returnUrl,
                Username = username
            };
            var apiCheck = _configuration["IdentityUrl"] + "/api/login";
            var request =
                new RestRequest(apiCheck, Method.Post);
            request.AddJsonBody(loginDto);

            var response = _client.ExecutePost(request);

            return response;
        }
    }

    public class LoginResult
    {
        public string returnUrl { get; set; }
        public string nonce { get; set; }
        public string sessionId { get; set; }
        public string username { get; set; }
    }

    public class AuthorizeDto
    {
        public string SignedHash { get; set; }
        public string PublicCertThumbprint { get; set; }
        public string SessionId { get; set; }
    }

    public class SyncAccountsDto
    {
        public string DeviceId { get; set; }
        public List<string> Guids { get; set; }
    }

    public class CheckDto
    {
        public string checkColor { get; set; }
        public string randomString { get; set; }
        public string returnUrl { get; set; }
        public string sessionId { get; set; }
        public string username { get; set; }
    }

    public class LoginDto
    {
        public string Username { get; set; }
        public string ReturnUrl { get; set; }
    }
}