using System.ComponentModel.DataAnnotations;

namespace WebApiDto.Auth
{
    public class UnlinkDeviceDto
    {
        [Required]
        [StringLength(64, MinimumLength = 64)]
        public string UnlinkToken { get; set; }
    }
}
