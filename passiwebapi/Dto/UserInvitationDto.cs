using NodaTime;
using System;

namespace passi_webapi.Dto
{
    public class UserInvitationDto
    {
        public DateTime CreationTime { get; set; }
        public long Id { get; set; }
        public long UserId { get; set; }
        public string Code { get; set; }
        public bool IsConfirmed { get; set; }
    }
}