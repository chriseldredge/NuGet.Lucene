using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Linq.Analysis;

namespace NuGet.Lucene
{
    public class DependencyAnalyzer : CaseInsensitiveKeywordAnalyzer
    {
        public override TokenStream TokenStream(string fieldName, TextReader reader)
        {
            var name = reader.ReadToEnd().Split(':')[0];
            return base.TokenStream(fieldName, new StringReader(name));
        }
    }
}