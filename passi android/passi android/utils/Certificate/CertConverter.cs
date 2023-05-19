using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace passi_android.utils.Certificate
{
    public class CertConverter : ICertConverter
    {
        public X509Certificate2 GetCertificateWithKey(MySecureString pin, AccountDb account)
        {
            return new X509Certificate2(Convert.FromBase64String(account.PrivateCertBinary), account.Salt + pin, X509KeyStorageFlags.Exportable);
        }
    }

    public interface ICertConverter
    {
        X509Certificate2 GetCertificateWithKey(MySecureString pin, AccountDb account);
    }
}