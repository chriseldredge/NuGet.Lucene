using System;
using System.Collections.Generic;
using System.Xml.Linq;
using AspNet.WebApi.HtmlMicrodataFormatter;
using NuGet.Lucene.Web.Models;

namespace NuGet.Lucene.Web.Formatters
{
    public class PackageSummaryListSerializer : DefaultSerializer
    {
        public override IEnumerable<Type> SupportedTypes
        {
            get { return new[] { typeof(IEnumerable<PackageVersionSummary>) }; }
        }

        public override IEnumerable<XObject> Serialize(string propertyName, object obj, SerializationContext context)
        {
            var table = new XElement("table",
                                     new XAttribute("id", "versionHistory"),
                                     new XAttribute("itemscope", "itemscope"),
                                     BuildTableHead(),
                                     BuildTableBody((IEnumerable<PackageVersionSummary>)obj, context));

            SetPropertyName(table, propertyName, context);

            yield return table;
        }

        private XElement BuildTableHead()
        {
            return new XElement("thead",
                                new XElement("th", new XText("Version")),
                                new XElement("th", new XText("Downloads")),
                                new XElement("th", new XText("Last updated")));
        }

        private XElement BuildTableBody(IEnumerable<PackageVersionSummary> packageSummaries, SerializationContext context)
        {
            var rows = new List<XObject>();

            foreach (var pkg in packageSummaries)
            {
                var tr = new XElement("tr",
                                      new XAttribute("itemscope", "itemscope"),
                                      new XAttribute("itemprop", "versions"),
                                      new XAttribute("itemtype", GetItemType(typeof(PackageVersionSummary), context)));

                tr.Add(new XElement("td", context.RootSerializer.Serialize("Link", pkg.Link, context)));
                tr.Add(new XElement("td", context.RootSerializer.Serialize("VersionDownloadCount", pkg.VersionDownloadCount, context)));
                tr.Add(new XElement("td", context.RootSerializer.Serialize("LastUpdated", pkg.LastUpdated, context)));

                rows.Add(tr);
            }

            return new XElement("tbody", rows);
        }
    }
}