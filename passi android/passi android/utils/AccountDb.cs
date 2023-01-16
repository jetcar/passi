namespace passi_android.utils
{
    public class AccountDb : AccountView
    {
        public string Password { get; set; }
        public string PrivateCertBinary { get; set; }
        public int pinLength { get; set; }
        public string PublicCertBinary { get; set; }
        public bool HaveFingerprint { get; set; }
    }
}