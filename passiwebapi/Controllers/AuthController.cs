using System;
using System.Collections.Generic;
using System.Linq;
using AppCommon;
using ConfigurationManager;
using Microsoft.AspNetCore.Mvc;
using Models;
using Newtonsoft.Json;
using NodaTime;
using Repos;
using Services;
using WebApiDto;
using WebApiDto.Auth;

namespace passi_webapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
            using (var transaction = _sessionsRepository.BeginTransaction())
            {
                _sessionsRepository.CancelSession(SessionId);
                transaction.Commit();
            }

            return Ok();
        }

        [HttpDelete, Route("Delete")]
        public IActionResult Delete([FromQuery] Guid accountGuid, [FromQuery] string thumbprint)
        {
            using (var transaction = _sessionsRepository.BeginTransaction())
            {
                _userRepository.DeleteAccount(accountGuid, thumbprint);
                transaction.Commit();
            }

            return Ok();
        }

        [HttpPost, Route("Start")]
        public LoginResponceDto Start([FromBody] StartLoginDto startLoginDto)
        {
            using (var transaction = _sessionsRepository.BeginTransaction())
            {
                if (!_userRepository.IsUsernameTaken(startLoginDto.Username))
                    throw new KeyNotFoundException("username not found");
                if (!_userRepository.IsUserFinished(startLoginDto.Username))
                    throw new BadRequestException("user not verified");
                var user = _userRepository.GetUser(startLoginDto.Username);
                var sessionDb = _sessionsRepository.BeginSession(startLoginDto.Username, startLoginDto.ClientId, startLoginDto.RandomString, startLoginDto.CheckColor.ToString(), startLoginDto.ReturnUrl);
                _firebaseService.SendNotification(user.Device.NotificationToken, "Passi login", JsonConvert.SerializeObject(
                        new FirebaseNotificationDto()
                        {
                            Sender = startLoginDto.ClientId,
                            ReturnHost = new Uri(startLoginDto.ReturnUrl).Host,
                            SessionId = sessionDb.Guid,
                            AccountGuid = user.Guid
                        }),
                    new Uri(startLoginDto.ReturnUrl).Host, sessionDb.Guid);
                transaction.Commit();

                return new LoginResponceDto() { SessionId = sessionDb.Guid };
            }
        }

        [HttpPost, Route("Authorize")]
        public LoginResponceDto Authorize([FromBody] AuthorizeDto authorizeDto)
        {
            using (var transaction = _sessionsRepository.BeginTransaction())
            {
                _sessionsRepository.VerifySession(authorizeDto.SessionId, authorizeDto.SignedHash, authorizeDto.PublicCertThumbprint);

                transaction.Commit();

                return new LoginResponceDto() { SessionId = authorizeDto.SessionId };
            }
        }

        [HttpGet, Route("check")]
        public CheckResponceDto Check([FromQuery] Guid sessionId)
        {
            using (var transaction = _sessionsRepository.BeginTransaction())
            {
                var sessionDb = _sessionsRepository.CheckSessionAndReturnUser(sessionId);
                if (sessionDb == null)
                    throw new BadRequestException("Session not found");
                if (sessionDb.Status == SessionStatus.Canceled)
                    throw new BadRequestException("User canceled");
                var universalTime = sessionDb.ExpirationTime.ToDateTimeUtc();
                if (DateTime.UtcNow > universalTime)
                    throw new BadRequestException("Request Expired");
                if (sessionDb.Status == SessionStatus.Error)
                    throw new BadRequestException(sessionDb.ErrorMessage);
                if (sessionDb.Status != SessionStatus.Confirmed)
                    throw new BadRequestException("Waiting for response");

                return new CheckResponceDto() { SignedHash = sessionDb.SignedHash, PublicCertThumbprint = sessionDb.PublicCertThumbprint, Username = sessionDb.User.EmailHash };
            }
        }

        [HttpPost, Route("GetActiveSession")]
        public IActionResult GetActiveSession([FromBody] GetAllSessionDto getSessionDto)
        {
            using (var transaction = _sessionsRepository.BeginTransaction())
            {
                var expirationTime = SystemClock.Instance.GetCurrentInstant();
                var sessionDb = _sessionsRepository.GetActiveSession(getSessionDto.DeviceId, expirationTime);
                if (sessionDb == null)
                    return Ok();

                return Ok(new NotificationDto()
                {
                    Sender = sessionDb.ClientId,
                    ReturnHost = new Uri(sessionDb.ReturnUrl).Host,
                    ConfirmationColor = Enum.Parse<Color>(sessionDb.CheckColor),
                    SessionId = sessionDb.Guid,
                    ExpirationTime = sessionDb.ExpirationTime.ToDateTimeUtc(),
                    RandomString = sessionDb.RandomString,
                    AccountGuid = sessionDb.User.Guid
                });
            }
        }

        [HttpPost, Route("SyncAccounts")]
        public List<AccountMinDto> SyncAccounts([FromBody] SyncAccountsDto syncAccountsDto)
        {
            using (var transaction = _sessionsRepository.BeginTransaction())
            {
                var accounts = _sessionsRepository.SyncAccounts(syncAccountsDto.Guids, syncAccountsDto.DeviceId);

                transaction.Commit();
                return accounts.Select(x => new AccountMinDto
                {
                    UserGuid = x.Guid,
                    Username = x.EmailHash
                }).ToList();
            }
        }
    }

    public class DateTimeKindMapper
    {
        public static DateTime Normalize(DateTime value)
            => DateTime.SpecifyKind(value, DateTimeKind.Utc);

        public static DateTime? NormalizeNullable(DateTime? value)
            => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : (DateTime?)null;

        public static object NormalizeObject(object value)
            => value is DateTime dateTime ? Normalize(dateTime) : value;
    }
}