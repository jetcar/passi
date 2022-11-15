using NodaTime;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class DeviceDb : BaseModel
    {
        public DeviceDb()
        {
            Users = new HashSet<UserDb>();
        }

        public long Id { get; set; }
        public string DeviceId { get; set; }
        public string NotificationToken { get; set; }
        public string Platform { get; set; }

        public virtual ICollection<UserDb> Users { get; set; }
    }
}