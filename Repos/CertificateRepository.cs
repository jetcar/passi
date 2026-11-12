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
                .FirstOrDefault(x => !x.IsRetired && x.Thumbprint == thumbprint && x.User.EmailHash.ToLower() == username.ToLower());
        }

        public CertificateDb AddCertificate(string certificateThumbprint, string PublicCert,
            string parentCertThumbprint, string deviceId, bool retireParent = false, string retirementSignature = null)
        {
            // The controller owns the transaction. Serialize rotations by parent before checking
            // for an existing child: a lost success response must be safe to replay verbatim.
            _dbContext.Database.ExecuteSqlRaw(
                "SELECT pg_advisory_xact_lock(1, hashtext({0}))", parentCertThumbprint);
            var parentState = _dbContext.Certificates.AsNoTracking()
                .FirstOrDefault(x => x.Thumbprint == parentCertThumbprint);
            if (parentState == null)
                throw new ClientException("Parent certificate not found");
            var existing = _dbContext.Certificates.AsNoTracking()
                .FirstOrDefault(x => x.Thumbprint == certificateThumbprint);
            if (existing != null)
            {
                if (existing.IsRetired)
                    throw new ClientException("Accepted successor certificate is retired; recover the account");
                if (existing.ParentCertId != parentCertThumbprint || existing.PublicCert != PublicCert ||
                    (retireParent && (!parentState.IsRetired || string.IsNullOrEmpty(retirementSignature) ||
                        existing.ParentCertSignature != retirementSignature)))
                    throw new ClientException("Certificate does not match the accepted rotation");

                // An exact replay acknowledges the prior commit; it must not re-enroll a device
                // or change the user's current device. Legacy non-retiring signatures do not
                // cover DeviceId and remain replayable after a later parent retirement.
                return existing;
            }
            // Re-read retirement under the lock, independently of the controller's tracked
            // parent loaded before locking. Concurrent different successors must not both win.
            if (parentState.IsRetired)
                throw new ClientException("Parent certificate is retired");
            if (retireParent && string.IsNullOrEmpty(retirementSignature))
                throw new ClientException("Retirement authorization is missing");
            if (!retireParent && _dbContext.Certificates.Any(x => x.ParentCertId == parentCertThumbprint))
                throw new ClientException("Parent certificate already has a successor");

            // Check-then-act race: two concurrent certificate-chain updates for the same brand-new
            // device id can both see "no row yet" below and each add their own Device (see
            // UserRepository.GetOrCreateDevice for the same race on the signup path). A
            // transaction-scoped Postgres advisory lock keyed on the device id serializes this
            // check-then-act per device id; the lock is released when the ambient transaction
            // (begun by the caller, e.g. CertificateController.UpdatePublicCert) commits or rolls back.
            _dbContext.Database.ExecuteSqlRaw("SELECT pg_advisory_xact_lock(hashtext({0})::bigint)", deviceId);

            if (retireParent && !_dbContext.UserDevices.Any(x => x.UserId == parentState.UserId && x.Device.DeviceId == deviceId))
                throw new ClientException("Retirement requires an already registered device");

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
                if (parentCert.IsRetired)
                    throw new ClientException("Parent certificate is retired");
                var certificateDb = new CertificateDb()
                {
                    ParentCertId = parentCertThumbprint,
                    PublicCert = PublicCert,
                    Thumbprint = certificateThumbprint,
                    UserId = parentCert.UserId,
                    // Persist the accepted v2 authorization for exact retirement receipts.
                    // Legacy non-retiring replay still needs only its already-enrolled child.
                    ParentCertSignature = retireParent ? retirementSignature : null,
                };
                parentCert.User.Device = device;
                parentCert.IsRetired = retireParent;

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

            throw new ClientException("Parent certificate not found");
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
            string newCertificateDeviceId, bool retireParent = false, string retirementSignature = null);

        CertificateDb GetCertificate(string parentCertThumbprint);
    }
}
