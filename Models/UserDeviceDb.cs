namespace Models
{
    public class UserDeviceDb : BaseModel
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public long DeviceId { get; set; }

        public virtual UserDb User { get; set; }
        public virtual DeviceDb Device { get; set; }
    }
}