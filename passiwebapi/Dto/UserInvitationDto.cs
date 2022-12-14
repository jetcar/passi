namespace passi_webapi.Dto
{
    public class UserInvitationDto
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string Code { get; set; }
        public bool IsConfirmed { get; set; }
    }
}