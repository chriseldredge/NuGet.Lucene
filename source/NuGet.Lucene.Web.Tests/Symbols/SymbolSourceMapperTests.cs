using System.Collections.Generic;
using NuGet.Lucene.Web.Symbols;
using NUnit.Framework;

namespace NuGet.Lucene.Web.Tests.Symbols
{
    [TestFixture]
    public class SymbolSourceMapperTests
    {
        private readonly ISet<string> sourceFiles = new HashSet<string>(new[]
        {
            @"IFooService.cs",
            @"DupeName.cs",
            @"impl\FooService.cs",
            @"impl\DupeName.cs"
        });

        [Test]
        [TestCase(@"c:\build\Foo\source\Foo\IFooService.cs", @"IFooService.cs")]
        [TestCase(@"c:\build\Foo\source\Foo\impl\FooService.cs", @"impl\FooService.cs")]
        [TestCase(@"c:\build\Foo\source\Foo\DupeName.cs", @"DupeName.cs")]
        [TestCase(@"c:\build\Foo\source\Foo\impl\DupeName.cs", @"impl\DupeName.cs")]
        [TestCase(@"c:\build\Foo\source\Foo\IMPL\FOOSERVICE.cs", @"impl\FooService.cs")]
        [TestCase(@"c:\IFooService.cs", @"IFooService.cs")]
        [TestCase(@"c:\Program Files\MSBuild\InjectedInclude.cs", @"")]
        public void FindSourceFile_Root(string referencedSource, string expected)
        {
            var result = new SymbolSourceMapper().FindSourceFile(referencedSource, sourceFiles);

            Assert.That(result, Is.EqualTo(expected).IgnoreCase, "For input \"" + referencedSource + "\"");
        }
    }
}
