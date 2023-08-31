using System.Linq;
using Microsoft.EntityFrameworkCore;
using Models;
using PostSharp.Extensibility;

namespace Repos
{
    [ReposProfile(AttributeTargetElements = MulticastTargets.Method)]
    public class CertificateRepository : BaseRepo<PassiDbContext>, ICertificateRepository
    {
        public CertificateRepository(PassiDbContext dbContext) : base(dbContext)
        {
        }

        public CertificateDb GetUserCertificate(string username, string thumbprint)
        {
            return _dbContext.Certificates
                .FirstOrDefault(x => x.Thumbprint == thumbprint && x.User.EmailHash == username);
        }

        public CertificateDb AddCertificate(string certificateThumbprint, string PublicCert,
            string parentCertThumbprint, string deviceId)
        {
            var device = _dbContext.Devices.FirstOrDefault(x => x.DeviceId == deviceId);
            if (device == null)
            {
                device = new DeviceDb()
                {
                    DeviceId = deviceId
                };
                _dbContext.Devices.Add(device);
            }

            var parentCert = _dbContext.Certificates.Include(x => x.User).FirstOrDefault(x => x.Thumbprint == parentCertThumbprint);
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