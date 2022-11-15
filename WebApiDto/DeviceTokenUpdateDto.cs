using System.ComponentModel.DataAnnotations;

namespace WebApiDto
{
    public class DeviceTokenUpdateDto
    {
        [Required]
        public string Token { get; set; }

        [Required]
        public string DeviceId { get; set; }
        [Required]
        public string Platform { get; set; }
    }
}
