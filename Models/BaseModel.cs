using NodaTime;
using System;

namespace Models
{
    public class BaseModel
    {
        public Instant CreationTime { get; set; }
        public Instant? ModifiedTime { get; set; }
        public UserDb ModifiedBy { get; set; }
        public long? ModifiedById { get; set; }
    }
}