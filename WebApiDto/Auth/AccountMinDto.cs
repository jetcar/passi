using System;
using System.ComponentModel.DataAnnotations;

namespace WebApiDto.Auth
{
    public class AccountMinDto
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public Guid UserGuid { get; set; }
    }

    public enum Status
    {
        active,
        inactive,
    }
}