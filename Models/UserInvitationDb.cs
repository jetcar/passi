using NodaTime;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    public class UserInvitationDb : BaseModel
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string Code { get; set; }
        public bool IsConfirmed { get; set; }
        public virtual UserDb User { get; set; }
    }
}