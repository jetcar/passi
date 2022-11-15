namespace WebApp.OpenId
{
    public class OpenIdDefaults
    {
        public const string AuthenticationScheme = "OpenId";

        public const string CallbackPath = "/oauth/callback";

        public const string SignedOutCallbackPath = "/oauth/logout";

        public const string SignedOutRedirectUri = "/";

        public const string ResponseType = "code";
    }
}