using System;

namespace NuGet.Lucene.Web.DataServices
{
    public interface IOperationContext
    {
        Uri CurrentRequestUri { get; }
        bool ClientDoesNotSpecifyOrder { get; }
        bool IsQueryForSpecificPackage(out string packageId, out SemanticVersion version);
    }
}