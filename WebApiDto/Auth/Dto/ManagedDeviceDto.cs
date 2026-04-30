using System;

namespace WebApiDto.Auth.Dto
{
    public class ManagedDeviceDto
    {
        public string DeviceId { get; set; }
        public string Platform { get; set; }
        public DateTime CreationTime { get; set; }
        public bool IsCurrent { get; set; }
    }
}