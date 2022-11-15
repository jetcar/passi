using System.Collections.Generic;

namespace WebApiDto.Auth
{
    public class SyncAccountsDto
    {
        public string DeviceId { get; set; }
        public List<string> Guids { get; set; }
    }
}