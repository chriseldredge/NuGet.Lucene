using System;
using AspNet.WebApi.HtmlMicrodataFormatter;

namespace NuGet.Lucene.Web.Formatters
{
    /// <summary>
    /// Extends <see cref="HtmlMicrodataFormatter"/> to include
    /// customizations that improve markup for packages and related content.
    /// </summary>
    public class NuGetHtmlMicrodataFormatter : HtmlMicrodataFormatter
    {
        public readonly PackageSummaryListSerializer PackageSummaryListSerializer = new PackageSummaryListSerializer();
        public readonly PackageHtmlSerializer PackageHtmlSerializer = new PackageHtmlSerializer();

        public NuGetHtmlMicrodataFormatter()
        {
            ToStringSerializer.AddSupportedTypes(typeof(Version), typeof(StrictSemanticVersion), typeof(IVersionSpec));

            RegisterSerializer(PackageSummaryListSerializer);
            RegisterSerializer(PackageHtmlSerializer);

            Title = GetType().Assembly.GetName().Name;
        }
    }
}