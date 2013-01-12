using Common.Logging;

namespace NuGet.Lucene.Web
{
    internal class UnhandledExceptionLogger
    {
        internal static readonly ILog Log = LogManager.GetLogger<UnhandledExceptionLogger>();
    }
}