using System;

namespace AppDto
{
    public class NotificationDto
    {
        public string Sender { get; set; }
        public Color ConfirmationColor { get; set; }
        public Guid SessionId { get; set; }
    }

    public enum Color
    {
        blue = 1,
        red = 2,
        green = 3,
        yellow = 4,
    }
}