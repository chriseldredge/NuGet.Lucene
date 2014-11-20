using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lucene.Net.Analysis.Tokenattributes;
using NuGet.Lucene.Analysis;
using NUnit.Framework;

namespace NuGet.Lucene.Tests.Analysis
{
    [TestFixture]
    class PackageIdAnalyzerTests
    {
        [Test]
        public void SingleWord()
        {
            Assert.That(GetTerms("Foo"), Is.EquivalentTo(new[] {"foo"}));
        }

        [Test]
        public void SingleNamespace()
        {
            Assert.That(GetTerms("Company.Foo"), Is.EquivalentTo(new[] { "foo", "company" }));
        }

        [Test]
        public void MultipleNamespace()
        {
            Assert.That(GetTerms("Company.Foo.Bar"), Is.EquivalentTo(new[] { "bar", "foo", "company" }));
        }

        IEnumerable<string> GetTerms(string value)
        {
            var s = new PackageIdAnalyzer().TokenStream("Id", new StringReader(value));
            try
            {
                while (s.IncrementToken())
                {
                    if (!s.HasAttribute<ITermAttribute>()) continue;
                    var attr = s.GetAttribute<ITermAttribute>();
                    yield return attr.Term;
                }
            }
            finally
            {
                s.Dispose();
            }
        }
    }
}
