using System.Collections;
using System.IO;
using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;

namespace NuGet.Lucene.Analysis
{
    public class PathAnalyzer : KeywordAnalyzer
    {
        public override TokenStream TokenStream(string fieldName, TextReader reader)
        {
            return new PathFilter(new LowerCaseFilter(base.TokenStream(fieldName, reader)));
        }

        sealed class PathFilter : TokenFilter
        {
            enum FilterState { Init, WholePath, File, FileWithoutExtension, FileParts }

            readonly ITermAttribute termAtt;
            FilterState currentState = FilterState.Init;
            IEnumerator fileParts;

            public PathFilter(TokenStream input)
                : base(input)
            {
                termAtt = AddAttribute<ITermAttribute>();
            }

            public override bool IncrementToken()
            {
                if (currentState == FilterState.Init)
                {
                    if (!input.IncrementToken())
                        return false;

                    NormalizeDirectorySeparators();
                }

                currentState = (FilterState) ((int) currentState + 1);

                switch (currentState)
                {
                    case FilterState.WholePath:
                        return true;
                    case FilterState.File:
                        return SetTermToFileName();
                    case FilterState.FileWithoutExtension:
                        return RemoveExtension();
                    default:
                        return NextFileNamePart();
                }
            }

            private void NormalizeDirectorySeparators()
            {
                var termBuffer = termAtt.TermBuffer();
                for (var i = termAtt.TermLength(); i >= 0; i--)
                {
                    if (termBuffer[i] == '\\')
                    {
                        termBuffer[i] = '/';
                    }
                }
            }

            private bool SetTermToFileName()
            {
                var termBuffer = termAtt.TermBuffer();
                for (var i = termAtt.TermLength(); i >= 0; i--)
                {
                    if (termBuffer[i] == '/')
                    {
                        var fileName = termBuffer.Take(termAtt.TermLength()).Skip(i + 1).ToArray();
                        termAtt.SetTermBuffer(fileName, 0, fileName.Length);
                        return true;
                    }
                }
                return false;
            }

            private bool RemoveExtension()
            {
                for (var i = termAtt.TermLength(); i >= 0; i--)
                {
                    var termBuffer = termAtt.TermBuffer();
                    if (termBuffer[i] == '.')
                    {
                        var fileName = termBuffer.Take(i).ToArray();
                        termAtt.SetTermBuffer(fileName, 0, fileName.Length);
                        return true;
                    }
                }
                return false;
            }

            private bool NextFileNamePart()
            {
                if (fileParts == null)
                {
                    fileParts = new string(termAtt.TermBuffer(), 0, termAtt.TermLength()).Split('.').GetEnumerator();
                }

                if (fileParts.MoveNext())
                {
                    termAtt.SetTermBuffer((string)fileParts.Current);
                    return true;
                }

                return false;
            }
        }
    }
}
