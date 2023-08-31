using System.ComponentModel.DataAnnotations;

namespace IdentityServer.Controllers.ViewInputs
{
    public class LoginInputModel
    {
        [Required]
        public string Username { get; set; }
        public bool RememberLogin { get; set; }
        public string ReturnUrl { get; set; }
        public string Nonce { get; set; }
    }
}