using Microsoft.AspNetCore.Mvc;
using Models;
using Newtonsoft.Json;
using NodaTime;

using Repos;
using System;
using System.Collections.Generic;
using System.Linq;
using GoogleTracer;
using NotificationsService;
using WebApiDto;
using WebApiDto.Auth;
using WebApiDto.Auth.Dto;
using Services;

namespace passi_webapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Profile]
    public class AuthController : ControllerBase
    {
        private ISessionsRepository _sessionsRepository;
        private IUserRepository _userRepository;
        private IFirebaseService _firebaseService;
        public AuthController(ISessionsRepository sessionsRepository, IUserRepository userRepository, IFirebaseService firebaseService)
        {
            _sessionsRepository = sessionsRepository;
            _userRepository = userRepository;
            _firebaseService = firebaseService;
        }

        [HttpGet, Route("Cancel")]
        public IActionResult Cancel([FromQuery] Guid SessionId)
        {
            _sessionsRepository.CancelSession(SessionId);

            return Ok();
        }

        [HttpDelete, Route("Delete")]
        public IActionResult Delete([FromQuery] Guid accountGuid, [FromQuery] string thumbprint)
        {
            _userRepository.DeleteAccount(accountGuid, thumbprint);

            return Ok();
        }

        [HttpPost, Route("Start")]
        public IActionResult Start([FromBody] StartLoginDto startLoginDto)
        {
            if (!_userRepository.IsUsernameTaken(startLoginDto.Username))
                return BadRequest("username not found");

            var user = _userRepository.GetUser(startLoginDto.Username);
            var registeredDevices = GetRegisteredDevices(user);
            if (!user.Certificates.Any() || !registeredDevices.Any())
                return BadRequest("No registered devices available for login approval");

            var sessionDb = _sessionsRepository.BeginSession(startLoginDto.Username, startLoginDto.ClientId,
                startLoginDto.RandomString, startLoginDto.CheckColor.ToString(), startLoginDto.ReturnUrl);
            var host = startLoginDto.ReturnUrl;
            try
            {
                host = new Uri(startLoginDto.ReturnUrl).Host;
            }
            catch (Exception)
            {
            }
            var notificationPayload = JsonConvert.SerializeObject(
                new FirebaseNotificationDto()
                {
                    Sender = startLoginDto.ClientId,
                    ReturnHost = host,
                    SessionId = sessionDb.Guid,
                    AccountGuid = user.Guid
                });
            var notificationTokens = registeredDevices
                .Select(x => x.NotificationToken)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            if (!notificationTokens.Any() && !string.IsNullOrWhiteSpace(user.Device?.NotificationToken))
            {
                notificationTokens.Add(user.Device.NotificationToken);
            }

            foreach (var notificationToken in notificationTokens)
            {
                _firebaseService.SendNotification(notificationToken, "Passi login", notificationPayload, host, sessionDb.Guid);
            }

            Guid sessionid = sessionDb.Guid;

            return Ok(new LoginResponceDto()
            {
                SessionId = sessionid,
                RegisteredDevices = registeredDevices.Select(GetDeviceDisplayName).ToList()
            });
        }

        private static List<DeviceDb> GetRegisteredDevices(UserDb user)
        {
            var registeredDevices = user.UserDevices
                .Select(x => x.Device)
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.DeviceId))
                .GroupBy(x => x.DeviceId)
                .Select(x => x.First())
                .ToList();

            if (user.Device != null
                && !string.IsNullOrWhiteSpace(user.Device.DeviceId)
                && registeredDevices.All(x => x.DeviceId != user.Device.DeviceId))
            {
                registeredDevices.Add(user.Device);
            }

            return registeredDevices;
        }

        private static string GetDeviceDisplayName(DeviceDb device)
        {
            var shortIdentifier = device.DeviceId.Length <= 10
                ? device.DeviceId
                : device.DeviceId[..8];

            var label = string.IsNullOrWhiteSpace(device.Platform)
                ? "Registered device"
                : device.Platform;

            return $"{label} ({shortIdentifier})";
        }

        [HttpPost, Route("Authorize")]
        public IActionResult Authorize([FromBody] AuthorizeDto authorizeDto)
        {
            var sessionDb = _sessionsRepository.CheckSessionAndReturnUser(authorizeDto.SessionId);
            if (sessionDb == null)
                return BadRequest("Session not found");
            _sessionsRepository.VerifySession(authorizeDto.SessionId, authorizeDto.SignedHash,
                authorizeDto.PublicCertThumbprint);

            return Ok(new LoginResponceDto() { SessionId = authorizeDto.SessionId });
        }

        [HttpGet, Route("check")]
        public IActionResult Check([FromQuery] Guid sessionId)
        {
            var sessionDb = _sessionsRepository.CheckSessionAndReturnUser(sessionId);
            if (sessionDb == null)
                return BadRequest("Session not found");
            if (sessionDb.Status == SessionStatus.Canceled)
                return BadRequest("User canceled");
            var universalTime = sessionDb.ExpirationTime;
            if (DateTime.UtcNow > universalTime)
                return BadRequest("Request Expired");
            if (sessionDb.Status == SessionStatus.Error)
                return BadRequest(sessionDb.ErrorMessage);
            if (sessionDb.Status != SessionStatus.Confirmed)
                return BadRequest("Waiting for response");

            //sessionDb.PublicCertThumbprint = "s";
            //sessionDb.SignedHash = "s";

            return Ok(new CheckResponceDto() { PublicCertThumbprint = sessionDb.PublicCertThumbprint, Username = sessionDb.Email });
        }

        [HttpGet, Route("session")]
        public IActionResult Session([FromQuery] Guid sessionId, string thumbprint, string username)
        {
            var sessionDb = _sessionsRepository.GetAuthorizedSession(sessionId, thumbprint, username);
            if (sessionDb == null)
                return BadRequest("Session not found");

            return Ok(new SessionMinDto() { SignedHash = sessionDb.SignedHashNew, PublicCert = sessionDb.User.Certificates.FirstOrDefault(x => x.Thumbprint == thumbprint)?.PublicCert, ExpirationTime = sessionDb.ExpirationTime.ToDateTimeUtc() });
        }

        [HttpPost, Route("GetActiveSession")]
        public IActionResult GetActiveSession([FromBody] GetAllSessionDto getSessionDto)
        {
            var expirationTime = SystemClock.Instance.GetCurrentInstant();
            var sessionDb = _sessionsRepository.GetActiveSession(getSessionDto.DeviceId, expirationTime);
            if (sessionDb == null)
                return Ok();
            var host = sessionDb.ReturnUrl;
            try
            {
                host = new Uri(sessionDb.ReturnUrl).Host;
            }
            catch (Exception)
            {
            }
            return Ok(new NotificationDto()
            {
                Sender = sessionDb.ClientId,
                ReturnHost = host,
                ConfirmationColor = Enum.Parse<Color>(sessionDb.CheckColor),
                SessionId = sessionDb.Guid,
                ExpirationTime = sessionDb.ExpirationTime,
                RandomString = sessionDb.RandomString,
                AccountGuid = sessionDb.UserGuid
            });
        }

        [HttpPost, Route("SyncAccounts")]
        public List<AccountMinDto> SyncAccounts([FromBody] SyncAccountsDto syncAccountsDto)
        {
            List<AccountMinDto> list = new List<AccountMinDto>();

            var accounts = _sessionsRepository.SyncAccounts(syncAccountsDto.Guids, syncAccountsDto.DeviceId);

            list = accounts.Select(x => new AccountMinDto
            {
                UserGuid = x.Guid,
                Username = x.EmailHash
            }).ToList();

            return list;
        }

        [HttpPost, Route("Devices")]
        public ActionResult<List<ManagedDeviceDto>> Devices([FromBody] ManageDevicesDto manageDevicesDto)
        {
            var devices = _userRepository.GetAccountDevices(manageDevicesDto.AccountGuid, manageDevicesDto.Thumbprint)
                .Select(x => new ManagedDeviceDto
                {
                    DeviceId = x.DeviceId,
                    Platform = x.Platform,
                    CreationTime = x.CreationTime.ToDateTimeUtc(),
                    IsCurrent = x.DeviceId == manageDevicesDto.CurrentDeviceId,
                })
                .ToList();

            return Ok(devices);
        }

        [HttpPost, Route("DeleteDevice")]
        public IActionResult DeleteDevice([FromBody] DeleteDeviceDto deleteDeviceDto)
        {
            _userRepository.DeleteDevice(
                deleteDeviceDto.AccountGuid,
                deleteDeviceDto.Thumbprint,
                deleteDeviceDto.DeviceId,
                deleteDeviceDto.CurrentDeviceId);

            return Ok();
        }
    }
}