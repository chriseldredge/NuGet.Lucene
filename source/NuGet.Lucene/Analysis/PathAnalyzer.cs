using System.IO;
using Lucene.Net.Analysis;

namespace NuGet.Lucene.Analysis
{
    public class PathAnalyzer : KeywordAnalyzer
    {
        public override TokenStream TokenStream(string fieldName, TextReader reader)
        {
            return new LowerCaseFilter(new PathTokenizer(reader));
        }

        public class PathTokenizer : CharTokenizer
        {
            public PathTokenizer(TextReader input)
                : base(input)
            {
            }

            protected override bool IsTokenChar(char c)
            {
                return c != '\\' && c != '/' && c != '.';
            }
        }
    }
}
