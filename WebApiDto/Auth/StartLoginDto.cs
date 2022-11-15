using System.ComponentModel.DataAnnotations;

namespace WebApiDto.Auth
{
    public class StartLoginDto
    {
        [Required] 
        public string Username { get; set; }
        [Required] 
        public string ClientId { get; set; }
        [Required] 
        public Color CheckColor { get; set; }
        [Required] 
        public string RandomString { get; set; }
        [Required]
        public string ReturnUrl { get; set; }
    }
}