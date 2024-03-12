using ConfigurationManager;
using Microsoft.EntityFrameworkCore;
using Models;
using NodaTime;

using System;
using System.Collections.Generic;
using System.Linq;
using GoogleTracer;
using RedisClient;

namespace Repos
{
    [Profile]
    public class SessionsRepository : BaseRepo<PassiDbContext>, ISessionsRepository
    {
        private AppSetting _appSetting;
        private int _sessionTimeout;
        private IRedisService _redisService;

        public SessionsRepository(PassiDbContext dbContext, AppSetting appSetting, IRedisService redisService) : base(dbContext)
        {
            _appSetting = appSetting;
            _redisService = redisService;
            _sessionTimeout = Convert.ToInt32(_appSetting["Timeout"]);
        }

        public SessionTempRecord BeginSession(string username, string clientId, string randomString, string color, string returnUrl)
        {
            var user = _dbContext.Users.First(x => x.EmailHash == username);
            var sessionDb = new SessionTempRecord()
            {
                Guid = Guid.NewGuid(),
                UserId = user.Id,
                ClientId = clientId,
                RandomString = randomString,
                CheckColor = color,
                ReturnUrl = returnUrl,
                Email = user.EmailHash,
                ExpirationTime = DateTime.UtcNow.AddMinutes(_sessionTimeout),
                UserGuid = user.Guid
            };
            _redisService.Add(sessionDb);
            _dbContext.Sessions.Add(new SimpleSessionDb()
            {
                Guid = sessionDb.Guid,
                UserId = sessionDb.UserId,
                ExpirationTime = Instant.FromDateTimeUtc(sessionDb.ExpirationTime)
            });
            _dbContext.SaveChanges();
            return sessionDb;
        }

        public SessionTempRecord CheckSessionAndReturnUser(Guid sessionId)
        {
            var userBySession = _redisService.Get(sessionId);

            return userBySession;
        }

        public void CancelSession(Guid guid)
        {
            _redisService.Delete(guid);
        }

        public void VerifySession(Guid guid, string signedHash, string publicCertThumbprint)
        {
            var session = _redisService.Get(guid);
            if (session != null)
            {
                var sessionDb = _dbContext.Sessions.FirstOrDefault(x => x.Guid == guid);
                if (sessionDb != null)
                {
                    sessionDb.SignedHashNew = signedHash;
                    _dbContext.SaveChanges();
                }
                session.PublicCertThumbprint = publicCertThumbprint;
                session.Status = SessionStatus.Confirmed;
                _redisService.Add(session);
            }
        }

        public SessionTempRecord GetActiveSession(string deviceId, Instant expirationTime)
        {
            var sessionDbs = _dbContext.Sessions.Include(x => x.User).Where(x => x.User.Device.DeviceId == deviceId).OrderByDescending(x => x.CreationTime).FirstOrDefault(x => x.Status != SessionStatus.Canceled && x.Status != SessionStatus.Confirmed && x.ExpirationTime >= expirationTime);
            if (sessionDbs != null)
                return _redisService.Get(sessionDbs.Guid);
            return null;
        }

        public List<UserDb> SyncAccounts(List<string> guilds, string deviceId)
        {
            var activeAccounts = new List<UserDb>();
            var existingAccounts = _dbContext.Users.Where(x => x.Device.DeviceId == deviceId).ToDictionary(x => x.Guid.ToString());
            foreach (var guid in guilds)
            {
                if (existingAccounts.ContainsKey(guid))
                    activeAccounts.Add(existingAccounts[guid]);
            }

            return activeAccounts;
        }

        public SessionTempRecord GetSessionById(Guid sessionId)
        {
            return _redisService.Get(sessionId);
        }

        public void Update(SessionTempRecord session)
        {
            _redisService.Add(session);
        }

        public SimpleSessionDb GetAuthorizedSession(Guid sessionId, string thunbprint, string username)
        {
            var certificate = _dbContext.Certificates.Include(x => x.User).FirstOrDefault(x => x.Thumbprint == thunbprint && x.User.EmailHash == username);
            var sessionDb = _dbContext.Sessions.FirstOrDefault(x => x.Guid == sessionId);
            if (certificate != null)
                return sessionDb;
            return null;
        }
    }

    public interface ISessionsRepository : ITransaction
    {
        SessionTempRecord BeginSession(string username, string clientId, string randomString, string color, string returnUrl);

        SessionTempRecord CheckSessionAndReturnUser(Guid sessionId);

        void CancelSession(Guid guid);

        void VerifySession(Guid guid, string signedHash, string publicCertThumbprint);

        SessionTempRecord GetActiveSession(string deviceId, Instant expirationTime);

        List<UserDb> SyncAccounts(List<string> guilds, string deviceId);

        SimpleSessionDb GetAuthorizedSession(Guid sessionId, string thunbprint, string username);
    }
}