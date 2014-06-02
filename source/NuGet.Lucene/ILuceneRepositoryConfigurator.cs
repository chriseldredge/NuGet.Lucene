using System;

namespace NuGet.Lucene
{
    public interface ILuceneRepositoryConfigurator : IDisposable
    {
        ILucenePackageRepository Repository { get; }
    }
}