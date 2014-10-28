using System.IO;
using Lucene.Net.Analysis;

namespace NuGet.Lucene.Analysis
{
    /// <summary>
    /// Normalize boolean strings like <c>"true"</c> to be capitalized like <c>"True"</c>
    /// </summary>
    public class BoolNormalizingAnalyzer : KeywordAnalyzer
    {
        public override TokenStream TokenStream(string fieldName, TextReader reader)
        {
            var token = reader.ReadToEnd();
            token = token.Substring(0, 1).ToUpperInvariant() + token.Substring(1);
            return base.TokenStream(fieldName, new StringReader(token));
        }
        
    }
}
