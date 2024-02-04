using System.Security.Claims;

namespace WebApp.Models
{
    public class UserInfoModel
    {
        public string UserId { get; set; }
        public string Thumbprint { get; set; }
        public string ValidFrom { get; set; }
        public string ValidTo { get; set; }
        public string SignedHash { get; set; }

        public static UserInfoModel Create(ClaimsIdentity identity)
        {
            return new UserInfoModel
            {
                UserId = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                Thumbprint = identity.FindFirst("Thumbprint")?.Value,
                ValidFrom = identity.FindFirst("ValidFrom")?.Value,
                ValidTo = identity.FindFirst("ValidTo")?.Value,
                PublicCert = identity.FindFirst("PublicCert")?.Value,
                SignedHash = identity.FindFirst("SignedHash")?.Value,
                Identity = identity
            };
        }

        public ClaimsIdentity Identity { get; set; }

        public string PublicCert { get; set; }
    }

    public class ClaimDto
    {
        public string Type { get; set; }
        public string Value { get; set; }
    }
}