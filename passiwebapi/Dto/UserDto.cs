using Models;
using System.Collections.Generic;
using System;

namespace passi_webapi.Dto
{
    public class UserDto
    {
        public long Id { get; set; }
        public string EmailHash { get; set; }
        public Guid Guid { get; set; }
        public long? DeviceId { get; set; }

        public virtual DeviceDb Device { get; set; }
        public virtual ICollection<CertificateDb> Certificates { get; set; }
        public virtual ICollection<UserInvitationDb> Invitations { get; set; }
        public virtual ICollection<SessionDb> SessionUsers { get; set; }
    }
}