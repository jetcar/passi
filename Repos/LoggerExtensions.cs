using log4net;
using System;
using System.Runtime.CompilerServices;

namespace Repos
{
    public static class LoggerExtensions
    {
        private const string MethodPropertyName = "Method";
        private const string FilePathPropertyName = "FilePath";
        private const string LineNumberPropertyName = "LineNumber";

        public static void LogAppError(this ILog logger, Exception exception, string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogWithContext(logger, () => logger.Error($"{message} [Method: {memberName}, File: {sourceFilePath}, Line: {sourceLineNumber}]", exception), memberName, sourceFilePath, sourceLineNumber);
        }

        public static void LogAppDebug(this ILog logger, string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogWithContext(logger, () => logger.Debug($"{message} [Method: {memberName}, File: {sourceFilePath}, Line: {sourceLineNumber}]"), memberName, sourceFilePath, sourceLineNumber);
        }

        public static void LogAppWarning(this ILog logger, string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            LogWithContext(logger, () => logger.Warn($"{message} [Method: {memberName}, File: {sourceFilePath}, Line: {sourceLineNumber}]"), memberName, sourceFilePath, sourceLineNumber);
        }

        private static void LogWithContext(ILog logger, Action logAction, string memberName, string sourceFilePath, int sourceLineNumber)
        {
            log4net.LogicalThreadContext.Properties[MethodPropertyName] = memberName;
            log4net.LogicalThreadContext.Properties[FilePathPropertyName] = sourceFilePath;
            log4net.LogicalThreadContext.Properties[LineNumberPropertyName] = sourceLineNumber;

            logAction();

            log4net.LogicalThreadContext.Properties.Remove(MethodPropertyName);
            log4net.LogicalThreadContext.Properties.Remove(FilePathPropertyName);
            log4net.LogicalThreadContext.Properties.Remove(LineNumberPropertyName);
        }
    }
}