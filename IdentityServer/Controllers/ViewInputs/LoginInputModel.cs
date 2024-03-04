using System.ComponentModel.DataAnnotations;

namespace IdentityServer.Controllers.ViewInputs
{
    public class LoginInputDto
    {
        [Required]
        public string Username { get; set; }

        public bool RememberLogin { get; set; }
        public string ReturnUrl { get; set; }
    }
}