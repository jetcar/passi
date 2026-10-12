using System;
using System.ComponentModel.DataAnnotations;

namespace WebApiDto.SignUp
{
    public class SignupDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string DeviceId { get; set; }

        [Required]
        public Guid? UserGuid { get; set; }
    }
}