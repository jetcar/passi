namespace WebApiDto.Auth
{
    public class CheckResponceDto
    {
        public string SignedHash { get; set; }
        public string Username { get; set; }
        public string PublicCertThumbprint { get; set; }
    }
}