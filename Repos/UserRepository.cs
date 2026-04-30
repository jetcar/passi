using Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using GoogleTracer;
using Microsoft.EntityFrameworkCore;

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
            var device = GetOrCreateDevice(user.Device.DeviceId, user.Device.Platform);
            user.DeviceId = device.Id;
            user.Device = device;
            EnsureUserDeviceLink(user, device);
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
            var userInvitationDb = _dbContext.Invitations
                .Include(x => x.User)
                .ThenInclude(x => x.UserDevices)
                .FirstOrDefault(x => x.User.EmailHash == email && x.Code == code);

            var device = GetOrCreateDevice(deviceId);

            if (userInvitationDb != null)
            {
                userInvitationDb.IsConfirmed = true;
                _dbContext.Certificates.Add(new CertificateDb()
                {
                    PublicCert = publicCert,
                    Thumbprint =
                        X509CertificateLoader.LoadCertificate(Convert.FromBase64String(publicCert)).Thumbprint,
                    User = userInvitationDb.User
                });

                userInvitationDb.User.Device = device;
                userInvitationDb.User.DeviceId = device.Id;
                EnsureUserDeviceLink(userInvitationDb.User, device);
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
            return _dbContext.Users
                .Include(x => x.Device)
                .Include(x => x.Certificates)
                .Include(x => x.UserDevices)
                .ThenInclude(x => x.Device)
                .First(x => x.EmailHash == username);
        }

        public IReadOnlyList<DeviceDb> GetAccountDevices(Guid accountGuid, string thumbprint)
        {
            var user = GetUserByAccount(accountGuid, thumbprint);
            return user.UserDevices
                .Select(x => x.Device)
                .OrderByDescending(x => x.CreationTime)
                .ToList();
        }

        public void DeleteDevice(Guid accountGuid, string thumbprint, string deviceId, string currentDeviceId)
        {
            if (deviceId == currentDeviceId)
            {
                throw new ClientException("Current device cannot be removed");
            }

            var user = GetUserByAccount(accountGuid, thumbprint);
            var link = user.UserDevices.FirstOrDefault(x => x.Device.DeviceId == deviceId);
            if (link == null)
            {
                throw new ClientException("Device not found");
            }

            _dbContext.UserDevices.Remove(link);

            if (user.DeviceId == link.DeviceId)
            {
                var replacementDeviceId = user.UserDevices
                    .Where(x => x.Id != link.Id)
                    .OrderByDescending(x => x.CreationTime)
                    .Select(x => (long?)x.DeviceId)
                    .FirstOrDefault();
                user.DeviceId = replacementDeviceId;
            }

            _dbContext.SaveChanges();

            CleanupDeviceIfUnused(link.DeviceId);
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

        private DeviceDb GetOrCreateDevice(string deviceId, string platform = null)
        {
            var device = _dbContext.Devices.FirstOrDefault(x => x.DeviceId == deviceId);
            if (device != null)
            {
                if (!string.IsNullOrWhiteSpace(platform) && string.IsNullOrWhiteSpace(device.Platform))
                {
                    device.Platform = platform;
                }

                return device;
            }

            device = new DeviceDb
            {
                DeviceId = deviceId,
                Platform = platform,
            };
            _dbContext.Devices.Add(device);
            return device;
        }

        private void EnsureUserDeviceLink(UserDb user, DeviceDb device)
        {
            if (user.UserDevices.Any(x => x.Device == device || x.DeviceId == device.Id))
            {
                return;
            }

            user.UserDevices.Add(new UserDeviceDb
            {
                User = user,
                Device = device,
            });
        }

        private UserDb GetUserByAccount(Guid accountGuid, string thumbprint)
        {
            var user = _dbContext.Users
                .Include(x => x.Certificates)
                .Include(x => x.UserDevices)
                .ThenInclude(x => x.Device)
                .FirstOrDefault(x => x.Guid == accountGuid && x.Certificates.Any(a => a.Thumbprint == thumbprint));

            if (user == null)
            {
                throw new ClientException("Account not found");
            }

            return user;
        }

        private void CleanupDeviceIfUnused(long deviceId)
        {
            var isReferenced = _dbContext.UserDevices.Any(x => x.DeviceId == deviceId)
                || _dbContext.Users.Any(x => x.DeviceId == deviceId);

            if (!isReferenced)
            {
                _dbContext.Devices.Where(x => x.Id == deviceId).DeleteFromQuery();
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

        IReadOnlyList<DeviceDb> GetAccountDevices(Guid accountGuid, string thumbprint);

        void DeleteDevice(Guid accountGuid, string thumbprint, string deviceId, string currentDeviceId);

        void AddInvitation(UserInvitationDb userInvitationDb);

        void DeleteAccount(Guid accountGuid, string thumbprint);

        void IncreaseFailedRetryCount(string email);

        string GetCode(string email);

        void DeleteAccount(Guid accountGuid);
    }
}