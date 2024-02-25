using System.ComponentModel.DataAnnotations;

namespace WebApiDto.SignUp
{
    public class SignupCheckDto
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Code { get; set; }
    }

    public class DeleteUserDto
    {
        [Required]
        public string Email { get; set; }
    }
}