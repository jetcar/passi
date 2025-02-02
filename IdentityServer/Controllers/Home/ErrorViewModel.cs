

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

    public class ErrorMessage
    {
        public string Error { get; set; }
    }
}