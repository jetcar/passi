using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Newtonsoft.Json;
using NodaTime;

using Repos;
using System;
using System.Collections.Generic;
using System.Linq;
using GoogleTracer;
using WebApiDto;
using WebApiDto.Auth;
using WebApiDto.Auth.Dto;

namespace passi_webapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Profile]
    public class AuthController : ControllerBase
    {
        private ISessionsRepository _sessionsRepository;
        private IUserRepository _userRepository;

        public AuthController(ISessionsRepository sessionsRepository, IUserRepository userRepository)
        {
            _sessionsRepository = sessionsRepository;
            _userRepository = userRepository;
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
            if (!_userRepository.IsUserFinished(startLoginDto.Username))
                return BadRequest("user not verified");
            var sessionDb = _sessionsRepository.BeginSession(startLoginDto.Username, startLoginDto.ClientId,
                startLoginDto.RandomString, startLoginDto.CheckColor.ToString(), startLoginDto.ReturnUrl);

            Guid sessionid = sessionDb.Guid;

            return Ok(new LoginResponceDto() { SessionId = sessionid });
        }

        [HttpPost, Route("Authorize")]
        public LoginResponceDto Authorize([FromBody] AuthorizeDto authorizeDto)
        {
            _sessionsRepository.VerifySession(authorizeDto.SessionId, authorizeDto.SignedHash,
                authorizeDto.PublicCertThumbprint);

            return new LoginResponceDto() { SessionId = authorizeDto.SessionId };
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
            catch (Exception e)
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
    }
}