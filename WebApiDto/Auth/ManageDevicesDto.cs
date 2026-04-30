using System;

namespace WebApiDto.Auth
{
    public class ManageDevicesDto
    {
        public Guid AccountGuid { get; set; }
        public string Thumbprint { get; set; }
        public string CurrentDeviceId { get; set; }
    }
}