using System;

namespace IdentityServer.Controllers.ViewModels
{

    public class CheckViewModel
    {
        private bool _needRefresh = true;
        public string Username { get; set; }
        public bool RememberLogin { get; set; }
        public string ReturnUrl { get; set; }
        public string CheckColor { get; set; }
        public Guid SessionId { get; set; }

        public bool NeedRefresh
        {
            get => _needRefresh;
            set => _needRefresh = value;
        }

        public string RandomString { get; set; }
    }
}