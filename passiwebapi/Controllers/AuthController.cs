using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Newtonsoft.Json;
using NodaTime;
using PostSharp.Extensibility;
using Repos;
using Services;
using System;
using System.Collections.Generic;
using System.Linq;
using WebApiDto;
using WebApiDto.Auth;
using WebApiDto.Auth.Dto;

namespace passi_webapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Profile(AttributeTargetElements = MulticastTargets.Method)]
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
            var strategy = _sessionsRepository.GetExecutionStrategy();
            strategy.Execute(() =>
            {
                using (var transaction = _sessionsRepository.BeginTransaction())
                {
                    _sessionsRepository.CancelSession(SessionId);
                    transaction.Commit();
                }
            });

            return Ok();
        }

        [HttpDelete, Route("Delete")]
        public IActionResult Delete([FromQuery] Guid accountGuid, [FromQuery] string thumbprint)
        {
            var strategy = _sessionsRepository.GetExecutionStrategy();
            strategy.Execute(() =>
            {
                using (var transaction = _sessionsRepository.BeginTransaction())
                {
                    _userRepository.DeleteAccount(accountGuid, thumbprint);
                    transaction.Commit();
                }
            });

            return Ok();
        }

        [HttpPost, Route("Start")]
        public LoginResponceDto Start([FromBody] StartLoginDto startLoginDto)
        {
            Guid sessionid = Guid.Empty;
            var strategy = _sessionsRepository.GetExecutionStrategy();
            strategy.Execute(() =>
            {
                using (var transaction = _sessionsRepository.BeginTransaction())
                {
                    if (!_userRepository.IsUsernameTaken(startLoginDto.Username))
                        throw new BadRequestException("username not found");
                    if (!_userRepository.IsUserFinished(startLoginDto.Username))
                        throw new BadRequestException("user not verified");
                    var sessionDb = _sessionsRepository.BeginSession(startLoginDto.Username, startLoginDto.ClientId,
                        startLoginDto.RandomString, startLoginDto.CheckColor.ToString(), startLoginDto.ReturnUrl);

                    transaction.Commit();

                    sessionid = sessionDb.Guid;
                }
            });
            return new LoginResponceDto() { SessionId = sessionid };
        }

        [HttpPost, Route("Authorize")]
        public LoginResponceDto Authorize([FromBody] AuthorizeDto authorizeDto)
        {
            var strategy = _sessionsRepository.GetExecutionStrategy();
            strategy.Execute(() =>
            {
                using (var transaction = _sessionsRepository.BeginTransaction())
                {
                    _sessionsRepository.VerifySession(authorizeDto.SessionId, authorizeDto.SignedHash,
                        authorizeDto.PublicCertThumbprint);

                    transaction.Commit();
                }
            });
            return new LoginResponceDto() { SessionId = authorizeDto.SessionId };
        }

        [HttpGet, Route("check")]
        public CheckResponceDto Check([FromQuery] Guid sessionId)
        {
            var sessionDb = _sessionsRepository.CheckSessionAndReturnUser(sessionId);
            if (sessionDb == null)
                throw new BadRequestException("Session not found");
            if (sessionDb.Status == SessionStatus.Canceled)
                throw new BadRequestException("User canceled");
            var universalTime = sessionDb.ExpirationTime;
            if (DateTime.UtcNow > universalTime)
                throw new BadRequestException("Request Expired");
            if (sessionDb.Status == SessionStatus.Error)
                throw new BadRequestException(sessionDb.ErrorMessage);
            if (sessionDb.Status != SessionStatus.Confirmed)
                throw new BadRequestException("Waiting for response");

            //sessionDb.PublicCertThumbprint = "s";
            //sessionDb.SignedHash = "s";

            return new CheckResponceDto() { PublicCertThumbprint = sessionDb.PublicCertThumbprint, Username = sessionDb.Email };
        }

        [HttpGet, Route("session")]
        public SessionMinDto Session([FromQuery] Guid sessionId, string thumbprint, string username)
        {
            var sessionDb = _sessionsRepository.GetAuthorizedSession(sessionId, thumbprint, username);
            if (sessionDb == null)
                throw new BadRequestException("Session not found");

            return new SessionMinDto() { SignedHash = sessionDb.SignedHashNew, PublicCert = sessionDb.User.Certificates.FirstOrDefault(x => x.Thumbprint == thumbprint)?.PublicCert, ExpirationTime = sessionDb.ExpirationTime.ToDateTimeUtc() };
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
            var strategy = _sessionsRepository.GetExecutionStrategy();
            strategy.Execute(() =>
            {
                using (var transaction = _sessionsRepository.BeginTransaction())
                {
                    var accounts = _sessionsRepository.SyncAccounts(syncAccountsDto.Guids, syncAccountsDto.DeviceId);

                    transaction.Commit();
                    list = accounts.Select(x => new AccountMinDto
                    {
                        UserGuid = x.Guid,
                        Username = x.EmailHash
                    }).ToList();
                }
            });
            return list;
        }
    }
}