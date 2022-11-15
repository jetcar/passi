namespace passi_android.utils
{
    public static class StorageKeys
    {
        public static string NotificationToken = "NotificationToken";
        public static string AllAccounts = "allAccounts";
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
