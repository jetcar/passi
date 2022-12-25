using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppCommon;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Models;

namespace Repos
{
    public class CertificateRepository : ICertificateRepository
    {
        private PassiDbContext _dbContext;

        public CertificateRepository(PassiDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IDbContextTransaction BeginTransaction()
        {
            return _dbContext.Database.BeginTransaction();
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

    public interface ICertificateRepository
    {
        public IDbContextTransaction BeginTransaction();

        CertificateDb GetUserCertificate(string username, string thumbprint);

        CertificateDb AddCertificate(string certificateThumbprint, string PublicCert, string parentCertThumbprint,
            string newCertificateDeviceId);

        CertificateDb GetCertificate(string parentCertThumbprint);
    }
}