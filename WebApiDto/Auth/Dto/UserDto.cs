using System;
using System.Collections.Generic;

namespace WebApiDto.Auth.Dto
{
    public class UserDto
    {
        public DateTime CreationTime { get; set; }
        public long Id { get; set; }
        public string EmailHash { get; set; }
        public Guid Guid { get; set; }
        public long? DeviceId { get; set; }

        public virtual DeviceDto Device { get; set; }
        public virtual ICollection<CertificateDto> Certificates { get; set; }
        public virtual ICollection<UserInvitationDto> Invitations { get; set; }
        public virtual ICollection<SessionDto> SessionUsers { get; set; }
    }
}