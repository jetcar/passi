using System.Linq;
using Models;
using GoogleTracer;
using Microsoft.EntityFrameworkCore;

namespace Repos
{
    [Profile]
    public class CertificateRepository : BaseRepo<PassiDbContext>, ICertificateRepository
    {
        public CertificateRepository(PassiDbContext dbContext) : base(dbContext)
        {
        }

        public CertificateDb GetUserCertificate(string username, string thumbprint)
        {
            return _dbContext.Certificates
                .FirstOrDefault(x => x.Thumbprint == thumbprint && x.User.EmailHash.ToLower() == username.ToLower());
        }

        public CertificateDb AddCertificate(string certificateThumbprint, string PublicCert,
            string parentCertThumbprint, string deviceId)
        {
            // Check-then-act race: two concurrent certificate-chain updates for the same brand-new
            // device id can both see "no row yet" below and each add their own Device (see
            // UserRepository.GetOrCreateDevice for the same race on the signup path). A
            // transaction-scoped Postgres advisory lock keyed on the device id serializes this
            // check-then-act per device id; the lock is released when the ambient transaction
            // (begun by the caller, e.g. CertificateController.UpdatePublicCert) commits or rolls back.
            _dbContext.Database.ExecuteSqlRaw("SELECT pg_advisory_xact_lock(hashtext({0})::bigint)", deviceId);

            var device = _dbContext.Devices.FirstOrDefault(x => x.DeviceId == deviceId);
            if (device == null)
            {
                device = new DeviceDb()
                {
                    DeviceId = deviceId
                };
                _dbContext.Devices.Add(device);
            }

            var parentCert = _dbContext.Certificates
                .Include(x => x.User)
                .ThenInclude(x => x.UserDevices)
                .FirstOrDefault(x => x.Thumbprint == parentCertThumbprint);
            if (parentCert != null)
            {
                var certificateDb = new CertificateDb()
                {
                    ParentCertId = parentCertThumbprint,
                    PublicCert = PublicCert,
                    Thumbprint = certificateThumbprint,
                    UserId = parentCert.UserId,
                };
                parentCert.User.Device = device;

                // Certificate chaining can enroll a brand-new device (an existing trusted device
                // signs the new device's certificate). That device must also be linked into
                // UserDevices - mirroring UserRepository.EnsureUserDeviceLink - or the user can
                // never see or remove it via normal device management (AuthController.Devices /
                // DeleteDevice only look at UserDevices).
                if (!parentCert.User.UserDevices.Any(x => x.Device == device || x.DeviceId == device.Id))
                {
                    parentCert.User.UserDevices.Add(new UserDeviceDb
                    {
                        User = parentCert.User,
                        Device = device,
                    });
                }

                _dbContext.Certificates.Add(certificateDb);
                _dbContext.SaveChanges();
                return certificateDb;
            }

            return null;
        }

        public CertificateDb GetCertificate(string parentCertThumbprint)
        {
            return _dbContext.Certificates.FirstOrDefault(x => x.Thumbprint == parentCertThumbprint);
        }
    }

    public interface ICertificateRepository : ITransaction
    {
        CertificateDb GetUserCertificate(string username, string thumbprint);

        CertificateDb AddCertificate(string certificateThumbprint, string PublicCert, string parentCertThumbprint,
            string newCertificateDeviceId);

        CertificateDb GetCertificate(string parentCertThumbprint);
    }
}