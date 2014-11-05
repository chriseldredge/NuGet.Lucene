using System;
using System.IO;

namespace NuGet.Lucene
{
    /// <summary>
    ///   An extension of <see cref="IPackage"/> that
    ///   can stream package contents reusing a single
    ///   stream without loading the entire package into
    ///   memory or using any temp files.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Implementations are <b>not</b> thread-safe.
    ///   </para>
    ///   <para>
    ///     <see cref="IFastZipPackage.Dispose"/> must be invoked
    ///     after any streams are accessed either via <see cref="GetZipEntryStream"/>
    ///     or via <see cref="IPackageFile.GetStream"/>.
    ///   </para>
    /// </remarks>
    public interface IFastZipPackage : IPackage, IDisposable
    {
        string GetFileLocation();
        Stream GetZipEntryStream(string path);
    }
}
