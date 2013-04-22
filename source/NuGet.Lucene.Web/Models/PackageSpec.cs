using System;
using System.Collections.Generic;
using System.IO;

namespace NuGet.Lucene.Web.Models
{
    public class PackageSpec : LocalPackage
    {
        public override Stream GetStream()
        {
            throw new NotSupportedException();
        }

        protected override IEnumerable<IPackageFile> GetFilesBase()
        {
            throw new NotSupportedException();
        }

        protected override IEnumerable<IPackageAssemblyReference> GetAssemblyReferencesBase()
        {
            throw new NotSupportedException();
        }
    }
}