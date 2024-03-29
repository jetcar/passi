﻿using System;
using System.Collections.Generic;

namespace Models
{
    public class UserDb : BaseModel
    {
        public UserDb()
        {
            CertificateModifiedBies = new HashSet<CertificateDb>();
            Certificates = new HashSet<CertificateDb>();
            Devices = new HashSet<DeviceDb>();
            InverseModifiedBy = new HashSet<UserDb>();
            InvitationModifiedBies = new HashSet<UserInvitationDb>();
            Invitations = new HashSet<UserInvitationDb>();
            SessionModifiedBies = new HashSet<SimpleSessionDb>();
            SessionUsers = new HashSet<SimpleSessionDb>();
        }

        public long Id { get; set; }
        public string EmailHash { get; set; }
        public Guid Guid { get; set; }
        public long? DeviceId { get; set; }

        public virtual DeviceDb Device { get; set; }
        public virtual ICollection<CertificateDb> CertificateModifiedBies { get; set; }
        public virtual ICollection<CertificateDb> Certificates { get; set; }
        public virtual ICollection<DeviceDb> Devices { get; set; }
        public virtual ICollection<UserDb> InverseModifiedBy { get; set; }
        public virtual ICollection<UserInvitationDb> InvitationModifiedBies { get; set; }
        public virtual ICollection<UserInvitationDb> Invitations { get; set; }
        public virtual ICollection<SimpleSessionDb> SessionModifiedBies { get; set; }
        public virtual ICollection<SimpleSessionDb> SessionUsers { get; set; }
    }
}