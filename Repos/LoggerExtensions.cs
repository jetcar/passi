using Serilog;
using Serilog.Context;
using System;
using System.Runtime.CompilerServices;

namespace Repos
{
    public static class LoggerExtensions
    {
        public static void LogAppError(this ILogger logger, Exception exception, string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            using var prop = LogContext.PushProperty("Method", memberName);
            LogContext.PushProperty("FilePath", sourceFilePath);
            LogContext.PushProperty("LineNumber", sourceLineNumber);
            logger.Error(exception, message);
        }

        public static void LogAppDebug(this ILogger logger, string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            using var prop = LogContext.PushProperty("Method", memberName);
            LogContext.PushProperty("FilePath", sourceFilePath);
            LogContext.PushProperty("LineNumber", sourceLineNumber);
            logger.Debug(message);
        }

        public static void LogAppWarning(this ILogger logger, string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            using var prop = LogContext.PushProperty("Method", memberName);
            LogContext.PushProperty("FilePath", sourceFilePath);
            LogContext.PushProperty("LineNumber", sourceLineNumber);
            logger.Warning(message);
        }
    }
}