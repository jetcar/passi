namespace AppDto
{
    public class SignupCheckDto
    {
        public string Code { get; set; }
        public string Email { get; set; }
    }
    public class SignupConfirmationDto
    {
        public string Code { get; set; }
        public string PublicCert { get; set; }
        public string Email { get; set; }
        public string Guid { get; set; }
    }
}