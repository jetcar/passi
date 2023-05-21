using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace passi_android.utils.Services.Certificate
{
    public class CertConverter : ICertConverter
    {
        public X509Certificate2 GetCertificateWithKey(MySecureString pin, AccountDb account)
        {
            var mySecureString = new MySecureString(account.Salt);
            if (pin != null)
                mySecureString.Append(pin);
            var secureStringToString = mySecureString.SecureStringToString();
            return new X509Certificate2(Convert.FromBase64String(account.PrivateCertBinary), secureStringToString, X509KeyStorageFlags.Exportable);
        }
    }

    public interface ICertConverter
    {
        X509Certificate2 GetCertificateWithKey(MySecureString pin, AccountDb account);
    }
}