using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http.Routing;
using System.Xml.Linq;
using AspNet.WebApi.HtmlMicrodataFormatter;
using NuGet.Lucene.Web.Models;

namespace NuGet.Lucene.Web.Formatters
{
    public class PackageHtmlSerializer : EntitySerializer<IPackage>
    {
        public PackageHtmlSerializer()
        {
            Property("Link", RenderLink);
            Property(pkg => pkg.Title, RenderTitle);
            Property(pkg => pkg.IconUrl, RenderIcon);
            Property(pkg => pkg.LicenseUrl, RenderLicense);
            Property(pkg => pkg.ReleaseNotes, RenderReleaseNotes);
            Property("VersionHistory", RenderVersionHistory);
        }

        private XElement RenderLink(IPackage package, SerializationContext context)
        {
            var urlHelper = new UrlHelper(context.Request);
            var href = urlHelper.Link(RouteNames.Packages.Info, new { package.Id, package.Version });
            var link = new Link(href, "canonical", "details");

            return new LinkSerializer().CreateAnchor("Link", link, context);
        }

        private XElement RenderVersionHistory(IPackage package, SerializationContext context)
        {
            var packageWithVersionHistory = package as PackageWithVersionHistory;

            if (packageWithVersionHistory != null)
            {
                return (XElement)context.Serialize("VersionHistory", packageWithVersionHistory.VersionHistory).Single();
            }

            var urlHelper = new UrlHelper(context.Request);
            var href = urlHelper.Link(RouteNames.Packages.Info, new { package.Id, package.Version }) + "#versionHistory";
            var link = new Link(href, "version history");

            return new LinkSerializer().CreateAnchor("VersionHistory", link, context);
        }

        private XElement RenderTitle(IPackage arg, SerializationContext context)
        {
            var title = arg.Title;

            if (string.IsNullOrEmpty(title))
            {
                title = arg.Id;
            }

            return new XElement("span",
                                new XAttribute("itemprop", "title"),
                                new XText(title));
        }

        private XElement RenderIcon(string propertyName, Uri uri, SerializationContext context)
        {
            if (uri == null) return null;

            var img = new XElement("img",
                                   new XAttribute("src", uri.GetComponents(UriComponents.AbsoluteUri, UriFormat.UriEscaped)),
                                   new XAttribute("alt", "Icon"));

            SetPropertyName(img, propertyName, context);

            return img;
        }

        private XElement RenderLicense(string propertyName, Uri arg, SerializationContext context)
        {
            if (arg == null || !arg.IsAbsoluteUri) return null;

            return new XElement("a",
                                new XAttribute("href", arg.GetComponents(UriComponents.AbsoluteUri, UriFormat.UriEscaped)),
                                new XAttribute("rel", "license"),
                                new XAttribute("itemprop", "licenseUrl"),
                                new XText(arg.GetComponents(UriComponents.AbsoluteUri, UriFormat.Unescaped)));
        }

        private XElement RenderReleaseNotes(string propName, string value, SerializationContext context)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            return new XElement("pre",
                                new XAttribute("itemprop", "releaseNotes"),
                                new XText(value));
        }

        protected override IEnumerable<KeyValuePair<string, object>> Reflect(object value)
        {
            var props = new List<KeyValuePair<string, object>>(base.Reflect(value));

            var k = props.Find(kv => kv.Key == "VersionHistory");

            if (props.Remove(k))
            {
                props.Add(k);
            }

            return props;
        }
    }
}