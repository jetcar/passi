using IdentityServer.Controllers.ViewInputs;

namespace IdentityServer.Controllers.ViewModels
{
    public class LogoutViewModel : LogoutInputModel
    {
        public bool ShowLogoutPrompt { get; set; } = true;
    }
}
