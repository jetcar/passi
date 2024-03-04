using System;

namespace IdentityServer.Controllers.ViewInputs
{
    public class CheckInputModel
    {
        public string Username { get; set; }
        public bool RememberLogin { get; set; }
        public string ReturnUrl { get; set; }
        public string CheckColor { get; set; }
        public Guid SessionId { get; set; }
        public string RandomString { get; set; }
    }
}