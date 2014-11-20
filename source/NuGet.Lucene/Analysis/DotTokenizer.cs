using System.IO;
using Lucene.Net.Analysis;

namespace NuGet.Lucene.Analysis
{
    public class DotTokenizer : CharTokenizer
    {
        public DotTokenizer(TextReader input)
            : base(input)
        {
        }

        protected override bool IsTokenChar(char c)
        {
            return c != '.';
        }
    }
}
