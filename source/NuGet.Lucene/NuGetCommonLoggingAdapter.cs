using System;
using System.Diagnostics;
using Common.Logging;

namespace NuGet.Lucene
{
    public class NuGetCommonLoggingAdapter : ILogger
    {
        public void Log(MessageLevel level, string message, params object[] args)
        {
            var calleeType = new StackTrace().GetFrame(1).GetMethod().DeclaringType ?? GetType();

            var logger = LogManager.GetLogger(calleeType);

            Action<Action<FormatMessageHandler>> logMethod;

            switch (level)
            {
                case MessageLevel.Debug:
                    logMethod = logger.Debug;
                    break;
                case MessageLevel.Info:
                    logMethod = logger.Info;
                    break;
                case MessageLevel.Error:
                    logMethod = logger.Error;
                    break;
                case MessageLevel.Warning:
                default:
                    logMethod = logger.Warn;
                    break;
            }

            logMethod(m => m(message, args));
        }

        public FileConflictResolution ResolveFileConflict(string message)
        {
            return FileConflictResolution.Ignore;
        }
    }
}