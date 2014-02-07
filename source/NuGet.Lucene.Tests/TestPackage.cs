using System;
using System.IO;

namespace NuGet.Lucene.Tests
{
    public class TestPackage : LucenePackage
    {
        public TestPackage()
            : base(_ => new MemoryStream())
        {
            SupportedFrameworks = new string[0];
        }

        public TestPackage(string id)
            : this(id, "1.0")
        {
        }

        public TestPackage(string id, string version)
            : this()
        {
            Id = id;
            Version = new StrictSemanticVersion(version);
        }
    }
}