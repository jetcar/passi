namespace MauiApp2.utils
{
    public static class StorageKeys
    {
        public const string NotificationToken = "NotificationToken";
        public const string AllAccounts = "allAccounts";
        public const string ProvidersKey = "providers";
    }

    public static class StringExt
    {
        public static string Truncate(this string variable, int Length)
        {
            if (string.IsNullOrEmpty(variable)) return variable;
            return variable.Length <= Length ? variable : variable.Substring(0, Length);
        }
    }
}