using Microsoft.EntityFrameworkCore;
using Models;

using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using GoogleTracer;

namespace Repos
{
    [Profile]
    public class UserRepository : BaseRepo<PassiDbContext>, IUserRepository
    {
        public UserRepository(PassiDbContext dbContext) : base(dbContext)
        {
        }

        public bool IsUsernameTaken(string username)
        {
            return _dbContext.Users.Any(x => x.EmailHash == username);
        }

        public UserDb AddUser(UserDb user)
        {
            var device = _dbContext.Devices.FirstOrDefault(x => x.DeviceId == user.Device.DeviceId);
            if (device != null)
            {
                user.DeviceId = device.Id;
                user.Device = device;
            }
            _dbContext.Users.Add(user);
            _dbContext.SaveChanges();
            return user;
        }

        public bool ValidateConfirmationCode(string email, string code)
        {
            var validateConfirmationCode = _dbContext.Invitations.FirstOrDefault(x => x.User.EmailHash == email && x.Code == code && x.IsConfirmed == false);
            return validateConfirmationCode != null && validateConfirmationCode.TryCount < 10;
        }

        public void ConfirmInvitation(string email, string publicCert, string guid, string code, string deviceId)
        {
            var userInvitationDb = _dbContext.Invitations.Include(x => x.User).FirstOrDefault(x => x.User.EmailHash == email && x.Code == code);

            var device = _dbContext.Devices.FirstOrDefault(x => x.DeviceId == deviceId);
            if (device == null)
            {
                device = new DeviceDb()
                {
                    DeviceId = deviceId
                };
                _dbContext.Devices.Add(device);
            }

            if (userInvitationDb != null)
            {
                userInvitationDb.IsConfirmed = true;
                _dbContext.Certificates.Add(new CertificateDb()
                {
                    PublicCert = publicCert,
                    Thumbprint =
                        new X509Certificate2(Convert.FromBase64String(publicCert)).Thumbprint,
                    User = userInvitationDb.User
                });

                userInvitationDb.User.Device = device;
                userInvitationDb.User.Guid = Guid.Parse(guid);
            }

            _dbContext.SaveChanges();
        }

        public void UpdateNotificationToken(string deviceId, string token, string platform)
        {
            var device = _dbContext.Devices.FirstOrDefault(x => x.DeviceId == deviceId || x.NotificationToken == token);
            if (device == null)
            {
                device = new DeviceDb()
                {
                    DeviceId = deviceId,
                    Platform = platform
                };
                _dbContext.Devices.Add(device);
            }

            device.NotificationToken = token;
            device.DeviceId = deviceId;
            _dbContext.SaveChanges();
        }

        public bool IsUserFinished(string username)
        {
            return _dbContext.Users.Any(x => x.EmailHash == username && x.Invitations.Any(a => a.IsConfirmed));
        }

        public UserDb GetUser(string username)
        {
            return _dbContext.Users.Include(x => x.Device).First(x => x.EmailHash == username);
        }

        public void AddInvitation(UserInvitationDb userInvitationDb)
        {
            _dbContext.RemoveRange(_dbContext.Invitations.Where(x => x.UserId == userInvitationDb.UserId));
            _dbContext.SaveChanges();
            _dbContext.Invitations.Add(userInvitationDb);
            _dbContext.SaveChanges();
        }

        public void DeleteAccount(Guid accountGuid, string thumbprint)
        {
            var exinstingUser = _dbContext.Users
                .Where(x => x.Certificates.Any(a => a.Thumbprint == thumbprint) && x.Guid == accountGuid).AsNoTracking()
                .FirstOrDefault();
            if (exinstingUser != null)
            {
                _dbContext.Sessions.Where(x => x.UserId == exinstingUser.Id).DeleteFromQuery();
                _dbContext.Certificates.Where(x => x.UserId == exinstingUser.Id).DeleteFromQuery();
                _dbContext.Invitations.Where(x => x.UserId == exinstingUser.Id).DeleteFromQuery();
                _dbContext.Users.Where(x => x.Id == exinstingUser.Id).DeleteFromQuery();
            }
        }

        public void IncreaseFailedRetryCount(string email)
        {
            var invitations = _dbContext.Invitations.Where(x => x.User.EmailHash == email).ToList();
            foreach (var userInvitationDb in invitations)
            {
                userInvitationDb.TryCount++;
            }

            _dbContext.SaveChanges();
        }

        public string GetCode(string email)
        {
            return _dbContext.Invitations.Where(x => x.User.EmailHash == email && x.IsConfirmed == false && x.TryCount < 10)
                .OrderByDescending(x => x.Id).Select(x => x.Code).FirstOrDefault();
        }

        public void DeleteAccount(Guid accountGuid)
        {
            var exinstingUser = _dbContext.Users.Where(x => x.Guid == accountGuid).AsNoTracking().FirstOrDefault();
            if (exinstingUser != null)
            {
                _dbContext.Sessions.Where(x => x.UserId == exinstingUser.Id).DeleteFromQuery();
                _dbContext.Certificates.Where(x => x.UserId == exinstingUser.Id).DeleteFromQuery();
                _dbContext.Invitations.Where(x => x.UserId == exinstingUser.Id).DeleteFromQuery();
                _dbContext.Users.Where(x => x.Id == exinstingUser.Id).DeleteFromQuery();
            }
        }
    }

    public interface IUserRepository : ITransaction
    {
        public bool IsUsernameTaken(string username);

        UserDb AddUser(UserDb user);

        bool ValidateConfirmationCode(string email, string code);

        void ConfirmInvitation(string email, string publicCert, string guid, string code, string deviceId);

        void UpdateNotificationToken(string deviceId, string token, string platform);

        bool IsUserFinished(string username);

        UserDb GetUser(string username);

        void AddInvitation(UserInvitationDb userInvitationDb);

        void DeleteAccount(Guid accountGuid, string thumbprint);

        void IncreaseFailedRetryCount(string email);

        string GetCode(string email);

        void DeleteAccount(Guid accountGuid);
    }
}