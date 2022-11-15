using System.ComponentModel.DataAnnotations;

namespace IdentityServer.Controllers.ClientRegistration;

public class ClientItemInput
{
    public int Id { get; set; }

    [Required]
    public string ClientId { get; set; }

    [Required]
    public string ReturnUrl { get; set; }

    public int ReturnUrlCount { get; set; }
    public bool Enabled { get; set; }

    [StringLength(maximumLength: 50, MinimumLength = 16)]
    public string ClientSecret { get; set; }

    [Required]
    public string Url { get; set; }
}