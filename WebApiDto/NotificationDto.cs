using System;
using WebApiDto.Auth;

namespace WebApiDto
{
    public class NotificationDto
    {
        public string Sender { get; set; }
        public Color ConfirmationColor { get; set; }
        public Guid SessionId { get; set; }
        public DateTime ExpirationTime { get; set; }
        public string RandomString { get; set; }
        public string ReturnHost { get; set; }
        public Guid AccountGuid { get; set; }
    }

    public class FirebaseNotificationDto
    {
        public string Sender { get; set; }
        public Guid SessionId { get; set; }
        public string ReturnHost { get; set; }
        public Guid AccountGuid { get; set; }
    }
}