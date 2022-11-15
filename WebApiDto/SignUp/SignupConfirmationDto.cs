using System.ComponentModel.DataAnnotations;

namespace WebApiDto.SignUp
{
    public class SignupConfirmationDto
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public string Code { get; set; }

        [Required]
        public string PublicCert { get; set; }

        [Required]
        public string Guid { get; set; }

        [Required]
        public string DeviceId { get; set; }
    }
}