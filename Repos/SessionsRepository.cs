using System;
using System.Collections.Generic;
using System.Linq;
using AppCommon;
using ConfigurationManager;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Storage;
using Models;
using NodaTime;

namespace Repos
{
    public class SessionsRepository : ISessionsRepository
    {
        private PassiDbContext _dbContext;
        private AppSetting _appSetting;
        private int _sessionTimeout;

        public SessionsRepository(PassiDbContext dbContext, AppSetting appSetting)
        {
            _dbContext = dbContext;
            _appSetting = appSetting;
            _sessionTimeout = Convert.ToInt32(_appSetting["Timeout"]);
        }

        public IDbContextTransaction BeginTransaction()
        {
            return _dbContext.Database.BeginTransaction();
        }

        public SessionDb BeginSession(string username, string clientId, string randomString, string color, string returnUrl)
        {
            var user = _dbContext.Users.First(x => x.EmailHash == username);
            var sessionDb = new SessionDb()
            {
                UserId = user.Id,
                ClientId = clientId,
                RandomString = randomString,
                CheckColor = color,
                ReturnUrl = returnUrl,
                ExpirationTime = SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromMinutes(_sessionTimeout))
            };
            _dbContext.Sessions.Add(sessionDb);
            _dbContext.SaveChanges();
            return sessionDb;
        }

        public SessionDb CheckSessionAndReturnUser(Guid sessionId)
        {
            var userBySession = _dbContext.Sessions.Include(x => x.User).FirstOrDefault(x => x.Guid == sessionId);

            return userBySession;
        }

        public void CancelSession(Guid guid)
        {
            var session = _dbContext.Sessions.FirstOrDefault(x => x.Guid == guid);
            if (session != null)
            {
                session.Status = SessionStatus.Canceled;
                _dbContext.SaveChanges();
            }
        }

        public void VerifySession(Guid guid, string signedHash, string publicCertThumbprint)
        {
            var session = _dbContext.Sessions.FirstOrDefault(x => x.Guid == guid);
            if (session != null)
            {
                session.SignedHash = signedHash;
                session.PublicCertThumbprint = publicCertThumbprint;
                session.Status = SessionStatus.Confirmed;
                _dbContext.SaveChanges();
            }
        }

        public SessionDb GetActiveSession(string deviceId, Instant expirationTime)
        {
            var sessionDbs = _dbContext.Sessions.Include(x => x.User).Where(x => x.User.Device.DeviceId == deviceId).FirstOrDefault(x => x.Status != SessionStatus.Canceled && x.Status != SessionStatus.Confirmed && x.ExpirationTime >= expirationTime);

            return sessionDbs;
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

        public SessionDb GetSessionById(Guid sessionId)
        {
            return _dbContext.Sessions.FirstOrDefault(x => x.Guid == sessionId);
        }

        public void Update(SessionDb session)
        {
            _dbContext.Sessions.Update(session);
        }
    }

    public interface ISessionsRepository
    {
        IDbContextTransaction BeginTransaction();

        SessionDb BeginSession(string username, string clientId, string randomString, string color, string returnUrl);

        SessionDb CheckSessionAndReturnUser(Guid sessionId);

        void CancelSession(Guid guid);

        void VerifySession(Guid guid, string signedHash, string publicCertThumbprint);

        SessionDb GetActiveSession(string deviceId, Instant expirationTime);

        List<UserDb> SyncAccounts(List<string> guilds, string deviceId);
    }
}