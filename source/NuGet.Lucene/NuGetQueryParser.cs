using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Linq;
using Lucene.Net.Linq.Mapping;
using Version = Lucene.Net.Util.Version;

namespace NuGet.Lucene
{
    /// <summary>
    /// A query parser that mimics the behavior specified at
    /// http://docs.nuget.org/docs/reference/search-syntax.
    ///
    /// Uses case-insensitive matching for field names and
    /// uses a default field when the specified field is not found.
    ///
    /// Treats the field <c>Id</c> as a partial match / fuzzy search
    /// and the field <c>PackageId</c> as an exact match.
    /// </summary>
    public class NuGetQueryParser : FieldMappingQueryParser<LucenePackage>
    {
        private const string DefaultSearchFieldName = "SearchId";

        private static readonly IDictionary<string, string> AliasMap =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"Id", "SearchId"},
                {"PackageId", "Id"}
            };

        public NuGetQueryParser(Version matchVersion, IDocumentMapper<LucenePackage> documentMapper)
            : base(matchVersion, DefaultSearchFieldName, documentMapper)
        {
        }

        public NuGetQueryParser(FieldMappingQueryParser<LucenePackage> parser)
            : this(parser.MatchVersion, parser.DocumentMapper)
        {
        }

        public static IDictionary<string, string> IndexedPropertyAliases
        {
            get {  return new Dictionary<string, string>(AliasMap); }
        }

        protected override IFieldMappingInfo GetMapping(string field)
        {
            string aliasTarget;
            if (AliasMap.TryGetValue(field, out aliasTarget))
            {
                field = aliasTarget;
            }

            field = DocumentMapper.IndexedProperties.FirstOrDefault(
                p => string.Equals(p, field, StringComparison.OrdinalIgnoreCase))
                    ?? DefaultSearchFieldName;

            return base.GetMapping(field);
        }
    }
}
