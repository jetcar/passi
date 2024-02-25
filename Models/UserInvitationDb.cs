namespace Models
{
    public class UserInvitationDb : BaseModel
    {
        private bool _isConfirmed;
        public long Id { get; set; }
        public long UserId { get; set; }
        public string Code { get; set; }

        public bool IsConfirmed
        {
            get => _isConfirmed;
            set => _isConfirmed = value;
        }

        public virtual UserDb User { get; set; }
        public int? TryCount { get; set; } = 0;
        public bool? Delete { get; set; }
    }
}