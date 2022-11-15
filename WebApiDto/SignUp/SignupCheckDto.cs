using System.ComponentModel.DataAnnotations;

namespace WebApiDto.SignUp
{
    public class SignupCheckDto
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public string Code { get; set; }
    }
}