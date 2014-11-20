using System.IO;
using Lucene.Net.Analysis;

namespace NuGet.Lucene.Analysis
{
    public class PackageIdAnalyzer : Analyzer
    {
        public override TokenStream TokenStream(string fieldName, TextReader reader)
        {
            return new LowerCaseFilter(new DotTokenizer(reader));
        }
    }
}
