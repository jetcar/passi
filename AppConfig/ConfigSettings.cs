namespace AppConfig
{
    public class ConfigSettings
    {
        public static string WebApiUrlLocal = "http://192.168.0.208/passiapi";
        public static string WebApiUrl = "https://passi.cloud/passiapi";

        public static string SignupPath = "/api/SignUp/signup";
        public static string SignupConfirmation = "/api/SignUp/confirm";
        public static string SignupCheck = "/api/SignUp/check";
        public static string TokenUpdate = "/api/Token/Update";
        public static string CancelCheck = "/api/Auth/Cancel";
        public static string Authorize = "/api/Auth/Authorize";
        public static string Time = "/api/Service/Time";
        public static string UpdateCertificate = "/api/Certificate/UpdatePublicCert";
        public static string CheckForStartedSessions = "/api/Auth/GetActiveSession";
        public static string SyncAccounts = "/api/Auth/SyncAccounts";
        public static string DeleteAccount = "/api/Auth/Delete";
    }
}