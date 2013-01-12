using System;
using System.ComponentModel;
using NuGet.Lucene.Mapping;

namespace NuGet.Lucene
{
    /// <summary>
    /// Decorates sealed class SemanticVersion from NuGet.Core with one that
    /// only considers two instances to be equal when their original strings
    /// are equal. SemanticVersion considers two instances to be equal when
    /// the Version part is semantically equal but formatted differently.
    /// For example, the versions 1.0 and 1.0.0 are considered equal by
    /// SemanticVersion, whereas this class treats them as not equal.
    /// </summary>
    [TypeConverter(typeof(CachingSemanticVersionConverter))]
    public class StrictSemanticVersion : IEquatable<StrictSemanticVersion>, IComparable<StrictSemanticVersion>, IComparable
    {
        private readonly string originalVersion;
        private readonly SemanticVersion semanticVersion;

        public StrictSemanticVersion(string version)
        {
            this.originalVersion = version;
            this.semanticVersion = new SemanticVersion(version);
        }

        public StrictSemanticVersion(SemanticVersion semanticVersion)
        {
            this.originalVersion = semanticVersion.ToString();
            this.semanticVersion = semanticVersion;
        }

        public SemanticVersion SemanticVersion
        {
            get { return semanticVersion; }
        }

        public bool Equals(StrictSemanticVersion other)
        {
            return ReferenceEquals(this, other) || (other != null && originalVersion.Equals(other.originalVersion, StringComparison.OrdinalIgnoreCase));
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || Equals(obj as StrictSemanticVersion);
        }

        public override int GetHashCode()
        {
            return ToString().ToLowerInvariant().GetHashCode();
        }

        public override string ToString()
        {
            return originalVersion;
        }

        public int CompareTo(StrictSemanticVersion other)
        {
            return semanticVersion.CompareTo(other.semanticVersion);
        }

        public int CompareTo(object obj)
        {
            return CompareTo((StrictSemanticVersion) obj);
        }

        public static bool operator ==(StrictSemanticVersion version1, StrictSemanticVersion version2)
        {
            if (ReferenceEquals(version1, null))
            {
                return ReferenceEquals(version2, null);
            }
            return version1.Equals(version2);
        }

        public static bool operator !=(StrictSemanticVersion version1, StrictSemanticVersion version2)
        {
            return !(version1 == version2);
        }

        public static bool operator <(StrictSemanticVersion version1, StrictSemanticVersion version2)
        {
            if (version1 == null)
            {
                throw new ArgumentNullException("version1");
            }
            return version1.CompareTo(version2) < 0;
        }

        public static bool operator <=(StrictSemanticVersion version1, StrictSemanticVersion version2)
        {
            return (version1 == version2) || (version1 < version2);
        }

        public static bool operator >(StrictSemanticVersion version1, StrictSemanticVersion version2)
        {
            if (version1 == null)
            {
                throw new ArgumentNullException("version1");
            }
            return version2 < version1;
        }

        public static bool operator >=(StrictSemanticVersion version1, StrictSemanticVersion version2)
        {
            return (version1 == version2) || (version1 > version2);
        }
    }
}