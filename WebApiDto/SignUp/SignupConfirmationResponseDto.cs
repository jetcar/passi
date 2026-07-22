namespace WebApiDto.SignUp
{
    public class SignupConfirmationResponseDto
    {
        /// <summary>
        /// The canonical account GUID as stored on the server. For an existing account this may
        /// differ from the GUID the client generated at enrollment; the client must adopt this
        /// value so login push notifications (keyed on the canonical GUID) match a local account.
        /// </summary>
        public string AccountGuid { get; set; }
    }
}
