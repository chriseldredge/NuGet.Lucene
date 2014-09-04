using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Util;

namespace NuGet.Lucene.Analysis
{
    public class PorterStemAnalyzer : StandardAnalyzer
    {
        public PorterStemAnalyzer(Version version) : base(version)
        {
        }

        public override TokenStream TokenStream(string fieldName, TextReader reader)
        {
            return new PorterStemFilter(base.TokenStream(fieldName, reader));
        }
    }
}
