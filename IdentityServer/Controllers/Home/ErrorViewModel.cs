using IdentityServer4.Models;
using IdentityServer4.Models.Messages;

namespace IdentityServer.Controllers.Home
{
    public class ErrorViewModel
    {
        public ErrorViewModel()
        {
        }

        public ErrorViewModel(string error)
        {
            Error = new ErrorMessage { Error = error };
        }

        public ErrorMessage Error { get; set; }
    }
}