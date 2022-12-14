using NodaTime;
using System;

namespace passi_webapi.Dto
{
    public class DeviceDto
    {
        public long Id { get; set; }
        public string DeviceId { get; set; }
        public string NotificationToken { get; set; }
        public string Platform { get; set; }
        public DateTime CreationTime { get; set; }
    }
}