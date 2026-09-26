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

        /// <summary>2-digit number the user matches on the phone (color kept for older app versions).</summary>
        public int? CheckNumber { get; set; }

        [Required]
        public string RandomString { get; set; }

        [Required]
        public string ReturnUrl { get; set; }
    }
}