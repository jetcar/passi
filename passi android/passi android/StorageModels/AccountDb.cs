using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;
using passi_android.ViewModels;

namespace passi_android.utils
{
    public class AccountDb : AccountViewModel
    {
        public string Salt { get; set; }
        public string PrivateCertBinary { get; set; }
        public int pinLength { get; set; }
        public string PublicCertBinary { get; set; }
        public bool HaveFingerprint { get; set; }
    }
}